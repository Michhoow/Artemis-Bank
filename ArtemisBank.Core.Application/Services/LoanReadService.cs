using ArtemisBank.Core.Application.Common.Models;
using ArtemisBank.Core.Application.Dtos.Common;
using ArtemisBank.Core.Application.Interfaces.Contracts;
using ArtemisBank.Core.Domain.Common.Enums;
using ArtemisBank.Core.Domain.Entities;
using ArtemisBank.Core.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ArtemisBank.Core.Application.Services
{
    public class LoanReadService : ILoanReadService
    {
        private readonly ILoanRepository _loanRepository;
        private readonly ILoanInstallmentRepository _installmentRepository;
        private readonly ILogger<LoanReadService> _logger;

        public LoanReadService(
            ILoanRepository loanRepository,
            ILoanInstallmentRepository installmentRepository,
            ILogger<LoanReadService> logger)
        {
            _loanRepository = loanRepository;
            _installmentRepository = installmentRepository;
            _logger = logger;
        }

        public async Task<LoanInfoDto?> GetByIdAsync(int loanId)
        {
            var loan = await _loanRepository.GetWithInstallmentsAsync(loanId);
            return loan == null ? null : Map(loan);
        }

        public async Task<LoanInfoDto?> GetByLoanNumberAsync(string loanNumber)
        {
            var loan = await _loanRepository.GetByLoanNumberAsync(loanNumber);
            return loan == null ? null : Map(loan);
        }

        public async Task<List<LoanInfoDto>> GetActiveByClientAsync(string clientId)
        {
            var loans = await _loanRepository.GetAllByClientAsync(clientId);
            return loans.Where(l => l.Status == LoanStatus.Activo).Select(Map).ToList();
        }

        public Task<int> CountActiveAsync() => _loanRepository.CountActiveAsync();

        public Task<decimal> GetPendingDebtByClientAsync(string clientId)
            => _loanRepository.GetPendingDebtByClientAsync(clientId);

        public Task<bool> LoanNumberExistsAsync(string number)
            => _loanRepository.LoanNumberExistsAsync(number);

        public async Task ApplyPaymentAsync(int loanId, decimal effectiveAmount,
            CancellationToken cancellationToken = default)
        {
            var remaining = Money.Round(effectiveAmount);
            if (remaining <= 0m) return;

            var loan = await _loanRepository.GetByIdAsync(loanId);
            if (loan == null)
            {
                _logger.LogWarning("Se intento aplicar un abono a un prestamo inexistente. Id {LoanId}.", loanId);
                return;
            }

            var pending = await _installmentRepository.GetPendingByLoanAsync(loanId, cancellationToken);
            var now = DateTime.Now;

            foreach (var installment in pending)
            {
                if (remaining <= 0m) break;

                var due = Money.Round(installment.TotalAmount - installment.PaidAmount);
                if (due <= 0m) continue;

                var applied = Money.Round(Math.Min(remaining, due));

                installment.PaidAmount = Money.Round(installment.PaidAmount + applied);
                remaining = Money.Round(remaining - applied);

                if (installment.PaidAmount >= installment.TotalAmount)
                {
                    installment.Status = InstallmentStatus.Pagada;
                    installment.PaidAt = now;

                    installment.IsOverdue = false;
                }
                else
                {
                    installment.Status = InstallmentStatus.ParcialmentePagada;
                }

                await _installmentRepository.UpdateEntityAsync(installment);
            }

            var totalApplied = Money.Round(Money.Round(effectiveAmount) - remaining);
            loan.PaidAmount = Money.Round(loan.PaidAmount + totalApplied);

            var stillPending = await _installmentRepository.GetPendingByLoanAsync(loanId, cancellationToken);
            if (stillPending.Count == 0)
            {
                loan.Status = LoanStatus.Completado;
                loan.CompletedAt = now;
                _logger.LogInformation("Prestamo {LoanNumber} saldado por completo.", loan.LoanNumber);
            }

            await _loanRepository.UpdateEntityAsync(loan);
        }

        private static LoanInfoDto Map(Loan loan) => new LoanInfoDto
        {
            Id = loan.Id,
            LoanNumber = loan.LoanNumber,
            ClientId = loan.ClientId,
            ApprovedCapital = loan.ApprovedCapital,
            PendingAmount = Money.Round(loan.PendingAmount),
            TotalInstallments = loan.TotalInstallments,
            PaidInstallments = loan.PaidInstallments,
            AnnualInterestRate = loan.AnnualInterestRate,
            TermInMonths = loan.TermInMonths,
            IsActive = loan.IsActive,
            IsOverdue = loan.IsOverdue
        };
    }
}
