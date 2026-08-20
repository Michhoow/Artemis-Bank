using ArtemisBank.Core.Application.Common.Models;
using ArtemisBank.Core.Application.Dtos.Common;
using ArtemisBank.Core.Application.Interfaces.Contracts;
using ArtemisBank.Core.Domain.Common.Enums;
using ArtemisBank.Core.Domain.Entities;
using ArtemisBank.Core.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ArtemisBank.Core.Application.Services
{
    /// <summary>
    /// Implementacion real del contrato ILoanReadService (reemplaza a PendingLoanReadService).
    /// Dueno: Manuel. La consume Michael para el pago a prestamo (cliente y cajero), los indicadores
    /// y la unicidad de numeros de 9 digitos.
    ///
    /// ApplyPaymentAsync recibe el monto EFECTIVO que Michael ya debito de la cuenta de origen dentro
    /// de la misma transaccion de base de datos: aqui solo se aplica sobre la tabla de amortizacion.
    /// </summary>
    public class LoanReadService : ILoanReadService
    {
        private readonly ILoanRepository _loanRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<LoanReadService> _logger;

        public LoanReadService(ILoanRepository loanRepository, IUnitOfWork unitOfWork,
            ILogger<LoanReadService> logger)
        {
            _loanRepository = loanRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<LoanInfoDto?> GetByIdAsync(int loanId)
        {
            var loan = await _loanRepository.GetByIdWithInstallmentsAsync(loanId);
            return loan == null ? null : ToInfo(loan);
        }

        public async Task<LoanInfoDto?> GetByLoanNumberAsync(string loanNumber)
        {
            var loan = await _loanRepository.GetByLoanNumberAsync(loanNumber);
            return loan == null ? null : ToInfo(loan);
        }

        public async Task<List<LoanInfoDto>> GetActiveByClientAsync(string clientId)
        {
            var loan = await _loanRepository.GetActiveByClientAsync(clientId);
            return loan == null ? new List<LoanInfoDto>() : new List<LoanInfoDto> { ToInfo(loan) };
        }

        public Task<int> CountActiveAsync() => _loanRepository.CountActiveAsync();

        public Task<decimal> GetPendingDebtByClientAsync(string clientId)
            => _loanRepository.GetActivePendingDebtByClientAsync(clientId);

        public Task<bool> LoanNumberExistsAsync(string number) => _loanRepository.LoanNumberExistsAsync(number);

        public async Task ApplyPaymentAsync(int loanId, decimal effectiveAmount,
            CancellationToken cancellationToken = default)
        {
            var loan = await _loanRepository.GetByIdWithInstallmentsAsync(loanId, cancellationToken);
            if (loan == null)
            {
                _logger.LogWarning("Se intento aplicar un abono a un prestamo inexistente {LoanId}.", loanId);
                return;
            }

            var remaining = Money.Round(effectiveAmount);

            // Se abona en orden de antiguedad (numero de cuota ascendente), consumiendo el pendiente de cada cuota.
            foreach (var installment in loan.Installments.OrderBy(i => i.InstallmentNumber))
            {
                if (remaining <= 0m) break;
                if (installment.Status == InstallmentStatus.Pagada) continue;

                var applied = Money.Round(Math.Min(remaining, installment.PendingAmount));
                installment.PendingAmount = Money.Round(installment.PendingAmount - applied);
                remaining = Money.Round(remaining - applied);

                installment.Status = installment.PendingAmount <= 0m
                    ? InstallmentStatus.Pagada
                    : InstallmentStatus.ParcialmentePagada;

                // Una cuota saldada deja de estar atrasada.
                if (installment.Status == InstallmentStatus.Pagada) installment.IsLate = false;
            }

            // Prestamo completado cuando ya no queda pendiente en ninguna cuota.
            if (loan.Installments.All(i => i.Status == InstallmentStatus.Pagada))
                loan.Status = LoanStatus.Completado;

            // NO se abre una transaccion nueva: Michael ya abrio una y la comparte via el mismo DbContext.
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        private static LoanInfoDto ToInfo(Loan loan) => new LoanInfoDto
        {
            Id = loan.Id,
            LoanNumber = loan.LoanNumber,
            ClientId = loan.ClientId,
            ApprovedCapital = loan.ApprovedCapital,
            PendingAmount = loan.PendingAmount,
            TotalInstallments = loan.TotalInstallments,
            PaidInstallments = loan.PaidInstallments,
            AnnualInterestRate = loan.AnnualInterestRate,
            TermInMonths = loan.TermInMonths,
            IsActive = loan.IsActive,
            IsOverdue = loan.IsOverdue
        };
    }
}
