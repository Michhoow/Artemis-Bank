using ArtemisBank.Core.Application.Common.Constants;
using ArtemisBank.Core.Application.Common.Exceptions;
using ArtemisBank.Core.Application.Interfaces;
using ArtemisBank.Core.Application.Interfaces.Contracts;
using ArtemisBank.Core.Application.ViewModels.Beneficiaries;
using ArtemisBank.Core.Domain.Common.Enums;
using ArtemisBank.Core.Domain.Entities;
using ArtemisBank.Core.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ArtemisBank.Core.Application.Services
{
    /// <summary>
    /// Beneficiarios frecuentes. Un beneficiario es SIEMPRE una cuenta activa de otro cliente.
    /// Eliminar solo rompe la relacion: nunca toca la cuenta ni el historial.
    /// </summary>
    public class BeneficiaryService : IBeneficiaryService
    {
        private readonly IBeneficiaryRepository _beneficiaryRepository;
        private readonly ISavingsAccountRepository _accountRepository;
        private readonly IUserReadService _userReadService;
        private readonly ILogger<BeneficiaryService> _logger;

        public BeneficiaryService(
            IBeneficiaryRepository beneficiaryRepository,
            ISavingsAccountRepository accountRepository,
            IUserReadService userReadService,
            ILogger<BeneficiaryService> logger)
        {
            _beneficiaryRepository = beneficiaryRepository;
            _accountRepository = accountRepository;
            _userReadService = userReadService;
            _logger = logger;
        }

        public async Task<List<BeneficiaryViewModel>> GetByClientAsync(string clientId,
            CancellationToken cancellationToken = default)
        {
            var beneficiaries = await _beneficiaryRepository.GetByClientAsync(clientId);
            if (beneficiaries.Count == 0) return new List<BeneficiaryViewModel>();

            var ownerIds = beneficiaries
                .Where(b => b.SavingsAccount != null)
                .Select(b => b.SavingsAccount!.ClientId)
                .Distinct();

            var owners = await _userReadService.GetByIdsAsync(ownerIds);
            var byId = owners.ToDictionary(o => o.Id, o => o);

            return beneficiaries.Select(b =>
            {
                var account = b.SavingsAccount;
                byId.TryGetValue(account?.ClientId ?? string.Empty, out var owner);
                return new BeneficiaryViewModel
                {
                    Id = b.Id,
                    SavingsAccountId = b.SavingsAccountId,
                    AccountNumber = account?.AccountNumber ?? string.Empty,
                    FirstName = owner?.FirstName ?? string.Empty,
                    LastName = owner?.LastName ?? string.Empty
                };
            }).ToList();
        }

        public async Task<BeneficiaryViewModel?> GetByIdAsync(string clientId, int beneficiaryId,
            CancellationToken cancellationToken = default)
        {
            var all = await GetByClientAsync(clientId, cancellationToken);
            return all.FirstOrDefault(b => b.Id == beneficiaryId);
        }

        public async Task AddAsync(string clientId, string accountNumber, CancellationToken cancellationToken = default)
        {
            var account = await _accountRepository.GetByAccountNumberAsync(accountNumber);

            if (account == null)
                throw new BusinessRuleException(AppMessages.InvalidAccountNumber);

            if (account.Status == AccountStatus.Cancelada)
                throw new BusinessRuleException(AppMessages.CancelledAccountAsBeneficiary);

            if (string.Equals(account.ClientId, clientId, StringComparison.Ordinal))
                throw new BusinessRuleException(AppMessages.OwnAccountAsBeneficiary);

            if (await _beneficiaryRepository.ExistsAsync(clientId, account.Id))
                throw new BusinessRuleException(AppMessages.BeneficiaryAlreadyRegistered);

            await _beneficiaryRepository.AddAsync(new Beneficiary
            {
                ClientId = clientId,
                SavingsAccountId = account.Id,
                CreatedAt = DateTime.Now,
                CreatedByUserId = clientId
            });

            _logger.LogInformation("Beneficiario ****{Last} agregado por el cliente {ClientId}",
                account.LastFourDigits, clientId);
        }

        public async Task DeleteAsync(string clientId, int beneficiaryId, CancellationToken cancellationToken = default)
        {
            var beneficiary = await _beneficiaryRepository.GetByIdAsync(beneficiaryId);

            if (beneficiary == null)
                throw new NotFoundException("El beneficiario indicado no existe.");

            // Cada cliente solo puede administrar sus propios beneficiarios.
            if (!string.Equals(beneficiary.ClientId, clientId, StringComparison.Ordinal))
                throw new ForbiddenException("No puede eliminar un beneficiario que no le pertenece.");

            await _beneficiaryRepository.DeleteAsync(beneficiaryId);

            _logger.LogInformation("Beneficiario {BeneficiaryId} eliminado por el cliente {ClientId}",
                beneficiaryId, clientId);
        }
    }
}
