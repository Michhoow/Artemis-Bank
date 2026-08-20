using ArtemisBank.Core.Application.Common.Constants;
using ArtemisBank.Core.Application.Dtos.Loans;
using ArtemisBank.Core.Application.Interfaces;
using FluentValidation;
using MediatR;

namespace ArtemisBank.Core.Application.Features.Loans.Commands
{
    /// <summary>POST /api/loan — asigna un prestamo a un cliente y desembolsa el capital.</summary>
    public class AssignLoanCommand : IRequest<LoanCreatedDto>
    {
        public string ClientId { get; set; } = string.Empty;
        public decimal CapitalAmount { get; set; }
        public int TermInMonths { get; set; }
        public decimal AnnualInterestRate { get; set; }
        /// <summary>Si es true, se permite asignar aunque el cliente resulte de alto riesgo.</summary>
        public bool ConfirmHighRisk { get; set; }
        /// <summary>Administrador autenticado (se toma del JWT).</summary>
        public string? AssignedByUserId { get; set; }
    }

    public class AssignLoanCommandValidator : AbstractValidator<AssignLoanCommand>
    {
        // 6..60, multiplos de 6.
        private static readonly int[] AllowedTerms = { 6, 12, 18, 24, 30, 36, 42, 48, 54, 60 };

        public AssignLoanCommandValidator()
        {
            RuleFor(c => c.ClientId)
                .NotEmpty().WithMessage(AppMessages.MustSelectClient);

            RuleFor(c => c.CapitalAmount)
                .GreaterThan(0m).WithMessage(AppMessages.LoanAmountGreaterThanZero);

            RuleFor(c => c.AnnualInterestRate)
                .GreaterThanOrEqualTo(0m).WithMessage(AppMessages.NegativeInterestRate);

            RuleFor(c => c.TermInMonths)
                .Must(t => AllowedTerms.Contains(t)).WithMessage(AppMessages.InvalidLoanTerm);
        }
    }

    public class AssignLoanCommandHandler : IRequestHandler<AssignLoanCommand, LoanCreatedDto>
    {
        private readonly ILoanService _service;

        public AssignLoanCommandHandler(ILoanService service) => _service = service;

        public Task<LoanCreatedDto> Handle(AssignLoanCommand request, CancellationToken cancellationToken)
            => _service.AssignAsync(new AssignLoanDto
            {
                ClientId = request.ClientId,
                CapitalAmount = request.CapitalAmount,
                TermInMonths = request.TermInMonths,
                AnnualInterestRate = request.AnnualInterestRate,
                ConfirmHighRisk = request.ConfirmHighRisk,
                AssignedByUserId = request.AssignedByUserId
            }, cancellationToken);
    }
}
