using ArtemisBank.Core.Application.Common.Constants;
using ArtemisBank.Core.Application.Common.Models;
using ArtemisBank.Core.Application.Dtos.Common;
using ArtemisBank.Core.Application.Interfaces;
using ArtemisBank.Core.Application.Interfaces.Contracts;
using ArtemisBank.Core.Domain.Common.Enums;
using ArtemisBank.Core.Domain.Entities;
using ArtemisBank.Core.Domain.Interfaces;
using ArtemisBank.Infrastructure.Identity.Entities;
using ArtemisBank.Infrastructure.Identity.Seeds;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ArtemisBank.Infrastructure.Identity.Services
{
    public class UserManagementService : IUserManagementService
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly IAccountNumberGenerator _accountNumberGenerator;
        private readonly ISavingsAccountRepository _accountRepository;
        private readonly ICommerceRepository _commerceRepository;
        private readonly ITransactionService _transactionService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<UserManagementService> _logger;

        public UserManagementService(
            UserManager<AppUser> userManager,
            IEmailService emailService,
            IAccountNumberGenerator accountNumberGenerator,
            ISavingsAccountRepository accountRepository,
            ICommerceRepository commerceRepository,
            ITransactionService transactionService,
            IHttpContextAccessor httpContextAccessor,
            ILogger<UserManagementService> logger)
        {
            _userManager = userManager;
            _emailService = emailService;
            _accountNumberGenerator = accountNumberGenerator;
            _accountRepository = accountRepository;
            _commerceRepository = commerceRepository;
            _transactionService = transactionService;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task<PagedResult<UserInfoDto>> GetPagedAsync(int page, int pageSize, string? role = null,
            CancellationToken cancellationToken = default)
        {
            if (page <= 0) page = 1;
            if (pageSize is <= 0 or > PagedResult<UserInfoDto>.MaxPageSize)
                pageSize = PagedResult<UserInfoDto>.MaxPageSize;

            var query = _userManager.Users.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(role))
            {
                var inRole = await _userManager.GetUsersInRoleAsync(role.Trim());
                var ids = inRole.Select(u => u.Id).ToHashSet(StringComparer.Ordinal);
                query = query.Where(u => ids.Contains(u.Id));
            }

            query = query.OrderBy(u => u.FirstName).ThenBy(u => u.LastName);

            var total = await query.CountAsync(cancellationToken);

            var users = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var dtos = new List<UserInfoDto>(users.Count);
            foreach (var user in users) dtos.Add(await ToDtoAsync(user));

            return PagedResult<UserInfoDto>.Create(dtos, page, pageSize, total);
        }

        public async Task<UserOperationResult> CreateAsync(CreateUserRequest request, string? createdByUserId,
            bool sendTokenInsteadOfLink = false, CancellationToken cancellationToken = default)
        {
            var role = NormalizeRole(request.Role);
            if (role == null)
                return UserOperationResult.Failure("El rol indicado no es válido.");

            if (role == DefaultRoles.Comercio)
                return UserOperationResult.Failure(
                    "Los usuarios con rol Comercio se crean desde el endpoint de comercios.");

            var validation = await ValidateUniquenessAsync(request);
            if (validation != null) return validation;

            if (request.Password != request.ConfirmPassword)
                return UserOperationResult.Failure(AppMessages.UserPasswordsDoNotMatch);

            var user = new AppUser
            {
                UserName = request.UserName.Trim(),
                Email = request.Email.Trim(),
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                Identification = Cedula.Normalize(request.Identification),

                IsActive = false,
                EmailConfirmed = false
            };

            var created = await _userManager.CreateAsync(user, request.Password);
            if (!created.Succeeded)
                return UserOperationResult.Failure(Describe(created));

            var assigned = await _userManager.AddToRoleAsync(user, role);
            if (!assigned.Succeeded)
            {
                await _userManager.DeleteAsync(user);
                return UserOperationResult.Failure(Describe(assigned));
            }

            string? warning = null;

            if (role == DefaultRoles.Cliente)
            {
                var accountWarning = await CreatePrincipalAccountAsync(
                    user, request.InitialAmount ?? 0m, createdByUserId, cancellationToken);
                warning ??= accountWarning;
            }

            var mailOk = await SendActivationAsync(user, sendTokenInsteadOfLink, cancellationToken);
            if (!mailOk) warning ??= AppMessages.UserActivationEmailFailed;

            _logger.LogInformation("Usuario '{UserName}' creado con rol '{Role}' por {AdminId}. Estado inicial: inactivo.",
                user.UserName, role, createdByUserId);

            return UserOperationResult.Success(user.Id, warning);
        }

        public async Task<UserOperationResult> CreateCommerceUserAsync(CreateUserRequest request, int commerceId,
            bool sendTokenInsteadOfLink = false, CancellationToken cancellationToken = default)
        {
            var commerce = await _commerceRepository.GetByIdAsync(commerceId);
            if (commerce == null)
                return UserOperationResult.Failure(AppMessages.CommerceNotFound);

            if (!string.IsNullOrWhiteSpace(commerce.UserId))
                return UserOperationResult.Failure(AppMessages.CommerceAlreadyHasUser);

            var validation = await ValidateUniquenessAsync(request);
            if (validation != null) return validation;

            if (request.Password != request.ConfirmPassword)
                return UserOperationResult.Failure(AppMessages.UserPasswordsDoNotMatch);

            var user = new AppUser
            {
                UserName = request.UserName.Trim(),
                Email = request.Email.Trim(),
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                Identification = Cedula.Normalize(request.Identification),
                IsActive = false,
                EmailConfirmed = false
            };

            var created = await _userManager.CreateAsync(user, request.Password);
            if (!created.Succeeded)
                return UserOperationResult.Failure(Describe(created));

            var assigned = await _userManager.AddToRoleAsync(user, DefaultRoles.Comercio);
            if (!assigned.Succeeded)
            {
                await _userManager.DeleteAsync(user);
                return UserOperationResult.Failure(Describe(assigned));
            }

            commerce.UserId = user.Id;

            if (string.IsNullOrWhiteSpace(commerce.AccountNumber))
            {
                var accountNumber = await _accountNumberGenerator.GenerateAsync(cancellationToken);
                await _accountRepository.AddAsync(new SavingsAccount
                {
                    AccountNumber = accountNumber,
                    ClientId = user.Id,
                    Balance = 0m,
                    Type = AccountType.Principal,
                    Status = AccountStatus.Activa,
                    CreatedAt = DateTime.Now,
                    CreatedByUserId = user.Id
                });
                commerce.AccountNumber = accountNumber;
            }

            await _commerceRepository.UpdateEntityAsync(commerce);

            var mailOk = await SendActivationAsync(user, sendTokenInsteadOfLink, cancellationToken);

            _logger.LogInformation("Usuario de comercio '{UserName}' creado y asociado al comercio {CommerceId}.",
                user.UserName, commerceId);

            return UserOperationResult.Success(user.Id,
                mailOk ? null : AppMessages.UserActivationEmailFailed);
        }

        public async Task<UserOperationResult> UpdateAsync(UpdateUserRequest request, string? updatedByUserId,
            CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(request.UserId);
            if (user == null) return UserOperationResult.Failure(AppMessages.UserNotFound);

            if (await UserNameExistsAsync(request.UserName, user.Id))
                return UserOperationResult.Failure(AppMessages.UserNameAlreadyExists);

            if (await EmailExistsAsync(request.Email, user.Id))
                return UserOperationResult.Failure(AppMessages.UserEmailAlreadyExists);

            if (await IdentificationExistsAsync(request.Identification, user.Id))
                return UserOperationResult.Failure(AppMessages.UserIdentificationAlreadyExists);

            user.FirstName = request.FirstName.Trim();
            user.LastName = request.LastName.Trim();
            user.Identification = Cedula.Normalize(request.Identification);
            user.Email = request.Email.Trim();
            user.UserName = request.UserName.Trim();

            var updated = await _userManager.UpdateAsync(user);
            if (!updated.Succeeded) return UserOperationResult.Failure(Describe(updated));

            if (!string.IsNullOrWhiteSpace(request.Password))
            {
                if (request.Password != request.ConfirmPassword)
                    return UserOperationResult.Failure(AppMessages.UserPasswordsDoNotMatch);

                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var reset = await _userManager.ResetPasswordAsync(user, token, request.Password);
                if (!reset.Succeeded) return UserOperationResult.Failure(Describe(reset));
            }

            string? warning = null;

            var additional = Money.Round(request.AdditionalAmount ?? 0m);
            if (additional > 0m)
            {
                var roles = await _userManager.GetRolesAsync(user);
                if (roles.Contains(DefaultRoles.Cliente))
                {
                    var principal = await _accountRepository.GetPrincipalByClientAsync(user.Id);
                    if (principal == null || !principal.IsActive)
                    {
                        warning = AppMessages.ClientNeedsPrincipalAccount;
                    }
                    else
                    {
                        var credit = await _transactionService.RegisterExternalCreditAsync(
                            new ExternalCreditRequest
                            {
                                TargetAccountNumber = principal.AccountNumber,
                                Amount = additional,
                                Operation = TransactionOperation.BalanceInicial,
                                OriginLabel = DisplayText.Deposit,
                                Actor = OperationActor.Of(updatedByUserId, Roles.Administrador)
                            }, cancellationToken);

                        if (!credit.Succeeded) warning = credit.ErrorMessage;
                    }
                }
            }

            _logger.LogInformation("Usuario '{UserName}' actualizado por {AdminId}.", user.UserName, updatedByUserId);

            return UserOperationResult.Success(user.Id, warning);
        }

        public async Task<UserOperationResult> SetActiveAsync(string targetUserId, bool isActive, string? callerUserId,
            CancellationToken cancellationToken = default)
        {
            if (!string.IsNullOrEmpty(callerUserId) &&
                string.Equals(callerUserId, targetUserId, StringComparison.Ordinal))
                return UserOperationResult.Failure(AppMessages.UserCannotModifySelf);

            var user = await _userManager.FindByIdAsync(targetUserId);
            if (user == null) return UserOperationResult.Failure(AppMessages.UserNotFound);

            user.IsActive = isActive;
            if (isActive) user.EmailConfirmed = true;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded) return UserOperationResult.Failure(Describe(result));

            _logger.LogInformation("Usuario '{UserName}' {Action} por {AdminId}.",
                user.UserName, isActive ? "activado" : "inactivado", callerUserId);

            return UserOperationResult.Success(user.Id);
        }

        public async Task<int> DeactivateUsersOfCommerceAsync(IEnumerable<string> userIds,
            CancellationToken cancellationToken = default)
        {
            var affected = 0;

            foreach (var userId in userIds.Where(id => !string.IsNullOrWhiteSpace(id)))
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null || !user.IsActive) continue;

                user.IsActive = false;
                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded) affected++;
            }

            return affected;
        }

        public async Task<bool> UserNameExistsAsync(string userName, string? excludeUserId = null)
        {
            if (string.IsNullOrWhiteSpace(userName)) return false;
            var normalized = userName.Trim().ToUpperInvariant();

            return await _userManager.Users.AnyAsync(u =>
                u.NormalizedUserName == normalized &&
                (excludeUserId == null || u.Id != excludeUserId));
        }

        public async Task<bool> EmailExistsAsync(string email, string? excludeUserId = null)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            var normalized = email.Trim().ToUpperInvariant();

            return await _userManager.Users.AnyAsync(u =>
                u.NormalizedEmail == normalized &&
                (excludeUserId == null || u.Id != excludeUserId));
        }

        public async Task<bool> IdentificationExistsAsync(string identification, string? excludeUserId = null)
        {
            if (string.IsNullOrWhiteSpace(identification)) return false;
            var normalized = identification.Trim();

            return await _userManager.Users.AnyAsync(u =>
                u.Identification == normalized &&
                (excludeUserId == null || u.Id != excludeUserId));
        }

        private async Task<UserOperationResult?> ValidateUniquenessAsync(CreateUserRequest request)
        {
            if (await UserNameExistsAsync(request.UserName))
                return UserOperationResult.Failure(AppMessages.UserNameAlreadyExists);

            if (await EmailExistsAsync(request.Email))
                return UserOperationResult.Failure(AppMessages.UserEmailAlreadyExists);

            if (await IdentificationExistsAsync(request.Identification))
                return UserOperationResult.Failure(AppMessages.UserIdentificationAlreadyExists);

            return null;
        }

        private async Task<string?> CreatePrincipalAccountAsync(AppUser user, decimal initialAmount,
            string? createdByUserId, CancellationToken cancellationToken)
        {
            try
            {
                var accountNumber = await _accountNumberGenerator.GenerateAsync(cancellationToken);

                await _accountRepository.AddAsync(new SavingsAccount
                {
                    AccountNumber = accountNumber,
                    ClientId = user.Id,

                    Balance = 0m,
                    Type = AccountType.Principal,
                    Status = AccountStatus.Activa,
                    CreatedAt = DateTime.Now,
                    CreatedByUserId = createdByUserId
                });

                var amount = Money.Round(initialAmount);
                if (amount > 0m)
                {
                    var credit = await _transactionService.RegisterExternalCreditAsync(new ExternalCreditRequest
                    {
                        TargetAccountNumber = accountNumber,
                        Amount = amount,
                        Operation = TransactionOperation.BalanceInicial,
                        OriginLabel = DisplayText.Deposit,
                        Actor = OperationActor.Of(createdByUserId, Roles.Administrador)
                    }, cancellationToken);

                    if (!credit.Succeeded) return credit.ErrorMessage;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No fue posible crear la cuenta principal del cliente '{UserName}'.",
                    user.UserName);
                return "El usuario fue creado, pero no fue posible generar su cuenta de ahorro principal.";
            }
        }

        private async Task<bool> SendActivationAsync(AppUser user, bool sendTokenInsteadOfLink,
            CancellationToken cancellationToken)
        {
            try
            {
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

                string subject, body;

                if (sendTokenInsteadOfLink)
                {
                    subject = EmailTemplates.ActivationTokenSubject;
                    body = EmailTemplates.ActivationTokenBody(user.FullName, token);
                }
                else
                {
                    var request = _httpContextAccessor.HttpContext?.Request;
                    var origin = request == null
                        ? string.Empty
                        : $"{request.Scheme}://{request.Host}";

                    var link = $"{origin}/Login/ConfirmAccount" +
                               $"?userId={Uri.EscapeDataString(user.Id)}" +
                               $"&token={Uri.EscapeDataString(token)}";

                    subject = EmailTemplates.ActivationSubject;
                    body = EmailTemplates.ActivationLinkBody(user.FullName, link);
                }

                return await _emailService.SendAsync(new EmailRequest
                {
                    To = user.Email ?? string.Empty,
                    Subject = subject,
                    HtmlBody = body
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No fue posible enviar el correo de activacion a '{UserName}'.",
                    user.UserName);
                return false;
            }
        }

        private static string? NormalizeRole(string? role)
        {
            if (string.IsNullOrWhiteSpace(role)) return null;

            return role.Trim().ToLowerInvariant() switch
            {
                "administrador" => DefaultRoles.Administrador,
                "cajero" => DefaultRoles.Cajero,
                "cliente" => DefaultRoles.Cliente,
                "comercio" => DefaultRoles.Comercio,
                _ => null
            };
        }

        private static string Describe(IdentityResult result)
            => string.Join(" ", result.Errors.Select(e => e.Description));

        private async Task<UserInfoDto> ToDtoAsync(AppUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);
            return new UserInfoDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Identification = user.Identification,
                Email = user.Email ?? string.Empty,
                UserName = user.UserName ?? string.Empty,
                Role = roles.FirstOrDefault() ?? string.Empty,
                IsActive = user.IsActive
            };
        }
    }
}
