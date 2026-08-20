using ArtemisBank.Core.Application.Common.Constants;
using ArtemisBank.Core.Application.Common.Exceptions;
using ArtemisBank.Core.Application.Common.Models;
using ArtemisBank.Core.Application.Dtos.Common;
using ArtemisBank.Core.Application.Dtos.CreditCards;
using ArtemisBank.Core.Application.Interfaces;
using ArtemisBank.Core.Application.Interfaces.Contracts;
using ArtemisBank.Core.Domain.Common.Enums;
using ArtemisBank.Core.Domain.Entities;
using ArtemisBank.Core.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ArtemisBank.Core.Application.Services
{
    /// <summary>
    /// Reglas de negocio de tarjetas de credito y avance de efectivo. Dueno: Manuel.
    ///
    /// Seguridad de datos sensibles:
    ///   - El numero de 16 digitos nunca sale del modulo (solo enmascarado / ultimos 4).
    ///   - El CVC se genera aleatorio, se guarda como hash SHA-256 y jamas se retorna.
    /// </summary>
    public class CreditCardService : ICreditCardService
    {
        private const decimal CashAdvanceInterestRate = 0.0625m; // 6.25%
        private const int MaxCardNumberAttempts = 25;
        private static readonly Random Randomizer = new Random();

        private readonly ICreditCardRepository _creditCardRepository;
        private readonly IUserReadService _userReadService;
        private readonly ISavingsAccountService _savingsAccountService;
        private readonly ITransactionService _transactionService;
        private readonly ICryptoService _cryptoService;
        private readonly IEmailService _emailService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CreditCardService> _logger;

        public CreditCardService(
            ICreditCardRepository creditCardRepository,
            IUserReadService userReadService,
            ISavingsAccountService savingsAccountService,
            ITransactionService transactionService,
            ICryptoService cryptoService,
            IEmailService emailService,
            IUnitOfWork unitOfWork,
            ILogger<CreditCardService> logger)
        {
            _creditCardRepository = creditCardRepository;
            _userReadService = userReadService;
            _savingsAccountService = savingsAccountService;
            _transactionService = transactionService;
            _cryptoService = cryptoService;
            _emailService = emailService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        // ================================================================== listados

        public async Task<PagedResult<CreditCardListItemDto>> GetPagedAsync(CreditCardFilterDto filter,
            CancellationToken cancellationToken = default)
            => (await SearchAsync(filter, cancellationToken)).Result;

        public async Task<(PagedResult<CreditCardListItemDto> Result, string? InfoMessage)> SearchAsync(
            CreditCardFilterDto filter, CancellationToken cancellationToken = default)
        {
            var page = filter.Page <= 0 ? 1 : filter.Page;
            var pageSize = NormalizePageSize(filter.PageSize);
            var status = NormalizeCardStatus(filter.Status);

            var query = _creditCardRepository.Query();
            string? infoMessage = null;
            var searchingByIdentification = !string.IsNullOrWhiteSpace(filter.Identification);

            if (searchingByIdentification)
            {
                var client = await _userReadService.GetByIdentificationAsync(filter.Identification!.Trim());
                if (client == null)
                    return (PagedResult<CreditCardListItemDto>.Empty(page, pageSize),
                        AppMessages.ClientNotFoundByIdentification);

                query = query.Where(c => c.ClientId == client.Id);
            }

            if (status == "activa") query = query.Where(c => c.Status == CreditCardStatus.Activa);
            else if (status == "cancelada") query = query.Where(c => c.Status == CreditCardStatus.Cancelada);

            var cards = await query.ToListAsync(cancellationToken);

            if (searchingByIdentification && string.IsNullOrWhiteSpace(filter.Status))
                cards = cards
                    .OrderBy(c => c.Status == CreditCardStatus.Activa ? 0 : 1)
                    .ThenByDescending(c => c.CreatedAt)
                    .ToList();
            else
                cards = cards.OrderByDescending(c => c.CreatedAt).ToList();

            if (searchingByIdentification && cards.Count == 0)
                infoMessage = AppMessages.ClientHasNoCards;

            var total = cards.Count;
            var pageItems = cards.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            var names = await ResolveNamesAsync(pageItems.Select(c => c.ClientId));

            var data = pageItems.Select(c => ToListItem(c, names)).ToList();
            return (PagedResult<CreditCardListItemDto>.Create(data, page, pageSize, total), infoMessage);
        }

        public async Task<CreditCardDetailDto?> GetDetailAsync(int creditCardId,
            CancellationToken cancellationToken = default)
        {
            var card = await _creditCardRepository.GetByIdWithConsumptionsAsync(creditCardId, cancellationToken);
            if (card == null) return null;

            var client = await _userReadService.GetByIdAsync(card.ClientId);

            return new CreditCardDetailDto
            {
                Id = card.Id,
                MaskedCardNumber = Mask(card.LastFourDigits),
                LastFourDigits = card.LastFourDigits,
                ClientId = card.ClientId,
                ClientFullName = client?.FullName ?? string.Empty,
                CreditLimit = card.CreditLimit,
                AvailableCredit = card.AvailableCredit,
                CurrentDebt = card.Debt,
                ExpirationDate = card.ExpirationDate,
                Status = CardStatusText(card.Status),
                Consumptions = card.Consumptions
                    .OrderByDescending(k => k.CreatedAt)
                    .Select(ToConsumptionDto)
                    .ToList()
            };
        }

        // ================================================================== asignacion

        public async Task<CreditCardListItemDto> AssignAsync(AssignCreditCardDto request,
            CancellationToken cancellationToken = default)
        {
            var limit = Money.Round(request.CreditLimit);
            if (limit <= 0m) throw new BusinessRuleException(AppMessages.CreditLimitGreaterThanZero);

            var client = await _userReadService.GetByIdAsync(request.ClientId)
                ?? throw new NotFoundException(AppMessages.ClientNotFoundByIdentification);

            if (!client.IsActive) throw new BusinessRuleException(AppMessages.OnlyActiveClientsForCard);

            var now = DateTime.Now;
            var cardNumber = await GenerateUniqueCardNumberAsync(cancellationToken);
            var cvc = Randomizer.Next(0, 1000).ToString("D3"); // 000..999

            var card = new CreditCard
            {
                CardNumber = cardNumber,
                ClientId = client.Id,
                CreditLimit = limit,
                Debt = 0m,
                CvcHash = _cryptoService.Hash(cvc),
                ExpirationDate = now.AddYears(3).ToString("MM/yy"),
                Status = CreditCardStatus.Activa,
                AssignedByUserId = request.AssignedByUserId,
                CreatedByUserId = request.AssignedByUserId,
                CreatedAt = now
            };

            await _creditCardRepository.AddAsync(card);

            _logger.LogInformation("Tarjeta ****{Last} asignada al cliente {ClientId}. Limite {Limit}.",
                card.LastFourDigits, client.Id, limit);

            var mailOk = await _emailService.SendAsync(new EmailRequest
            {
                To = client.Email,
                Subject = EmailTemplates.CardAssignedSubject,
                HtmlBody = EmailTemplates.CardAssignedBody(client.FullName, card.LastFourDigits, limit,
                    card.ExpirationDate, now)
            }, cancellationToken);

            if (!mailOk)
                _logger.LogWarning("La tarjeta ****{Last} se creo, pero fallo el correo de notificacion.",
                    card.LastFourDigits);

            var names = new Dictionary<string, string> { [client.Id] = client.FullName };
            return ToListItem(card, names);
        }

        // ================================================================== edicion de limite

        public async Task UpdateLimitAsync(int creditCardId, decimal newLimit,
            CancellationToken cancellationToken = default)
        {
            var limit = Money.Round(newLimit);
            var card = await _creditCardRepository.GetByIdAsync(creditCardId)
                ?? throw new NotFoundException(AppMessages.CardDoesNotExist);

            if (!card.IsActive) throw new BusinessRuleException(AppMessages.CannotModifyCancelledCard);
            if (limit <= 0m) throw new BusinessRuleException(AppMessages.CreditLimitGreaterThanZero);
            if (limit < card.Debt) throw new BusinessRuleException(AppMessages.NewLimitBelowDebt);

            card.CreditLimit = limit;
            await _creditCardRepository.UpdateEntityAsync(card);

            _logger.LogInformation("Limite de la tarjeta ****{Last} actualizado a {Limit}.", card.LastFourDigits, limit);

            var client = await _userReadService.GetByIdAsync(card.ClientId);
            var mailOk = await _emailService.SendAsync(new EmailRequest
            {
                To = client?.Email ?? string.Empty,
                Subject = EmailTemplates.CardLimitUpdatedSubject,
                HtmlBody = EmailTemplates.CardLimitUpdatedBody(client?.FullName ?? string.Empty, card.LastFourDigits,
                    limit, DateTime.Now)
            }, cancellationToken);

            if (!mailOk)
                _logger.LogWarning("Se actualizo el limite de la tarjeta ****{Last}, pero fallo el correo.",
                    card.LastFourDigits);
        }

        // ================================================================== cancelacion

        public async Task CancelAsync(int creditCardId, CancellationToken cancellationToken = default)
        {
            var card = await _creditCardRepository.GetByIdAsync(creditCardId)
                ?? throw new NotFoundException(AppMessages.CardDoesNotExist);

            if (!card.IsActive) throw new BusinessRuleException(AppMessages.CannotModifyCancelledCard);
            if (card.HasDebt) throw new BusinessRuleException(AppMessages.CardHasPendingDebt);

            card.Status = CreditCardStatus.Cancelada;
            await _creditCardRepository.UpdateEntityAsync(card);

            _logger.LogInformation("Tarjeta ****{Last} cancelada. El historial de consumos se conserva.",
                card.LastFourDigits);
        }

        // ================================================================== avance de efectivo

        public async Task<OperationResult> CashAdvanceAsync(CashAdvanceDto request,
            CancellationToken cancellationToken = default)
        {
            var amount = Money.Round(request.Amount);
            if (amount <= 0m) return OperationResult.Failure(AppMessages.AdvanceAmountGreaterThanZero);

            var card = await _creditCardRepository.GetByCardNumberAsync(request.CardNumber, cancellationToken);
            if (card == null) return OperationResult.Failure(AppMessages.InvalidCardNumber);

            // La tarjeta debe pertenecer al cliente autenticado.
            if (!string.IsNullOrEmpty(request.ClientId) &&
                !string.Equals(card.ClientId, request.ClientId, StringComparison.Ordinal))
                return OperationResult.Failure(AppMessages.InvalidCardNumber);

            if (!card.IsActive) return OperationResult.Failure(AppMessages.CardNotActive);
            if (IsExpired(card.ExpirationDate)) return OperationResult.Failure(AppMessages.CardExpired);

            var targetAccount = await _savingsAccountService.GetEntityByNumberAsync(request.TargetAccountNumber,
                cancellationToken);
            if (targetAccount == null || !targetAccount.IsActive)
                return OperationResult.Failure(AppMessages.AdvanceAccountNotActive);

            if (!string.IsNullOrEmpty(request.ClientId) &&
                !string.Equals(targetAccount.ClientId, request.ClientId, StringComparison.Ordinal))
                return OperationResult.Failure(AppMessages.InvalidAccountNumber);

            var interest = Money.Round(amount * CashAdvanceInterestRate);
            var totalCharge = Money.Round(amount + interest);

            // El total (monto + interes) no puede superar el credito disponible.
            if (totalCharge > card.AvailableCredit)
            {
                await RegisterRejectedConsumptionAsync(card, totalCharge, "AVANCE", null,
                    AppMessages.AdvanceExceedsAvailableCredit, cancellationToken);
                return OperationResult.Failure(AppMessages.AdvanceExceedsAvailableCredit);
            }

            var now = DateTime.Now;

            await using (var dbTransaction = await _unitOfWork.BeginTransactionAsync(cancellationToken))
            {
                try
                {
                    // Acredita SOLO el monto solicitado a la cuenta destino (el interes no se acredita).
                    var credit = await _transactionService.RegisterExternalCreditAsync(new ExternalCreditRequest
                    {
                        TargetAccountNumber = targetAccount.AccountNumber,
                        Amount = amount,
                        Operation = TransactionOperation.AvanceEfectivo,
                        OriginLabel = card.LastFourDigits,
                        Actor = OperationActor.Of(request.ClientId, Roles.Cliente)
                    }, cancellationToken);

                    if (!credit.Succeeded)
                        return OperationResult.Failure(credit.ErrorMessage ?? AppMessages.AdvanceAccountNotActive);

                    // Carga a la tarjeta el monto + interes y registra el consumo AVANCE.
                    card.Debt = Money.Round(card.Debt + totalCharge);
                    await _creditCardRepository.UpdateEntityAsync(card);

                    await AddConsumptionAsync(card.Id, totalCharge, "AVANCE", null,
                        ConsumptionStatus.Aprobado, null, now, request.ClientId, cancellationToken);

                    await dbTransaction.CommitAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    await dbTransaction.RollbackAsync(cancellationToken);
                    _logger.LogError(ex, "Fallo el avance de efectivo de la tarjeta ****{Last}.", card.LastFourDigits);
                    throw;
                }
            }

            _logger.LogInformation("Avance de efectivo aprobado. Tarjeta ****{Last}. Monto {Amount}. Interes {Interest}.",
                card.LastFourDigits, amount, interest);

            var client = await _userReadService.GetByIdAsync(card.ClientId);
            var mailOk = await _emailService.SendAsync(new EmailRequest
            {
                To = client?.Email ?? string.Empty,
                Subject = EmailTemplates.CashAdvanceSubject(card.LastFourDigits),
                HtmlBody = EmailTemplates.CashAdvanceBody(client?.FullName ?? string.Empty, amount, interest,
                    totalCharge, card.LastFourDigits, targetAccount.LastFourDigits, now)
            }, cancellationToken);

            return OperationResult.Success(null, mailOk ? null : AppMessages.PaymentOkMailFailed);
        }

        // ================================================================== helpers de consumo

        private async Task AddConsumptionAsync(int cardId, decimal amount, string commerceName, int? commerceId,
            ConsumptionStatus status, string? rejectionReason, DateTime moment, string? actorUserId,
            CancellationToken cancellationToken)
        {
            var consumption = new Consumption
            {
                CreditCardId = cardId,
                Amount = amount,
                CommerceName = commerceName,
                CommerceId = commerceId,
                Status = status,
                RejectionReason = rejectionReason,
                CreatedAt = moment,
                CreatedByUserId = actorUserId
            };

            // SaveChanges dentro de una transaccion abierta es seguro (misma unidad de trabajo).
            await _creditCardRepository.AddConsumptionAsync(consumption, cancellationToken);
        }

        private async Task RegisterRejectedConsumptionAsync(CreditCard card, decimal amount, string commerceName,
            int? commerceId, string reason, CancellationToken cancellationToken)
        {
            // Un rechazo se registra pero NO modifica deuda ni balances.
            await AddConsumptionAsync(card.Id, amount, commerceName, commerceId, ConsumptionStatus.Rechazado,
                reason, DateTime.Now, null, cancellationToken);

            _logger.LogInformation("Consumo RECHAZADO en la tarjeta ****{Last}. Motivo: {Reason}.",
                card.LastFourDigits, reason);
        }

        // ================================================================== helpers varios

        private async Task<string> GenerateUniqueCardNumberAsync(CancellationToken cancellationToken)
        {
            for (var attempt = 0; attempt < MaxCardNumberAttempts; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // 16 digitos como texto: se permiten ceros a la izquierda.
                var builder = new System.Text.StringBuilder(16);
                lock (Randomizer)
                {
                    for (var i = 0; i < 16; i++) builder.Append(Randomizer.Next(0, 10));
                }
                var candidate = builder.ToString();

                if (!await _creditCardRepository.CardNumberExistsAsync(candidate, cancellationToken))
                    return candidate;
            }

            throw new ConflictException(AppMessages.CouldNotGenerateCardNumber);
        }

        private static bool IsExpired(string expiration)
        {
            // expiration = MM/yy
            if (string.IsNullOrWhiteSpace(expiration) || !expiration.Contains('/')) return true;
            var parts = expiration.Split('/');
            if (parts.Length != 2) return true;
            if (!int.TryParse(parts[0], out var month) || !int.TryParse(parts[1], out var yy)) return true;

            var year = 2000 + yy;
            // Vence al final del mes de expiracion.
            var lastValidDay = new DateTime(year, month, 1).AddMonths(1).AddDays(-1);
            return DateTime.Now.Date > lastValidDay;
        }

        private static int NormalizePageSize(int pageSize)
        {
            if (pageSize <= 0) return PagedResult<CreditCardListItemDto>.MaxPageSize;
            return pageSize > PagedResult<CreditCardListItemDto>.MaxPageSize
                ? PagedResult<CreditCardListItemDto>.MaxPageSize
                : pageSize;
        }

        private static string NormalizeCardStatus(string? status)
        {
            var value = (status ?? "activa").Trim().ToLowerInvariant();
            return value is "activa" or "cancelada" or "todas" ? value : "activa";
        }

        private static string CardStatusText(CreditCardStatus status) => status == CreditCardStatus.Activa ? "Activa" : "Cancelada";

        private static string Mask(string lastFour) => "************" + lastFour;

        private async Task<Dictionary<string, string>> ResolveNamesAsync(IEnumerable<string> clientIds)
        {
            var ids = clientIds.Distinct().ToList();
            if (ids.Count == 0) return new Dictionary<string, string>();
            var users = await _userReadService.GetByIdsAsync(ids);
            return users.ToDictionary(u => u.Id, u => u.FullName);
        }

        private static CreditCardListItemDto ToListItem(CreditCard card, IReadOnlyDictionary<string, string> names)
            => new CreditCardListItemDto
            {
                Id = card.Id,
                MaskedCardNumber = Mask(card.LastFourDigits),
                LastFourDigits = card.LastFourDigits,
                ClientId = card.ClientId,
                ClientFullName = names.TryGetValue(card.ClientId, out var name) ? name : string.Empty,
                CreditLimit = card.CreditLimit,
                AvailableCredit = card.AvailableCredit,
                CurrentDebt = card.Debt,
                ExpirationDate = card.ExpirationDate,
                Status = CardStatusText(card.Status),
                CreatedAt = card.CreatedAt
            };

        private static ConsumptionDto ToConsumptionDto(Consumption k) => new ConsumptionDto
        {
            Id = k.Id,
            Date = k.CreatedAt,
            Amount = k.Amount,
            CommerceName = k.CommerceName,
            Status = k.Status == ConsumptionStatus.Aprobado ? "APROBADO" : "RECHAZADO"
        };
    }
}
