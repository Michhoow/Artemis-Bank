using ArtemisBank.Core.Application.Common.Constants;
using ArtemisBank.Core.Application.Common.Models;
using ArtemisBank.Core.Application.Dtos.Loans;
using ArtemisBank.Core.Application.Interfaces;
using FluentValidation;
using MediatR;

namespace ArtemisBank.Core.Application.Features.Loans.Commands
{
    public class CreateLoanCommand : IRequest<LoanDetailDto>
    {
        public string ClientId { get; set; } = string.Empty;
        public decimal CapitalAmount { get; set; }
        public int TermInMonths { get; set; }
        public decimal AnnualInterestRate { get; set; }

        public bool ConfirmHighRisk { get; set; }

        public string? AdminUserId { get; set; }
    }

    public class CreateLoanCommandValidator : AbstractValidator<CreateLoanCommand>
    {
        public CreateLoanCommandValidator()
        {
            RuleFor(c => c.ClientId)
                .NotEmpty().WithMessage(AppMessages.LoanClientRequired);

            RuleFor(c => c.CapitalAmount)
                .GreaterThan(0m).WithMessage(AppMessages.LoanAmountGreaterThanZero);

            RuleFor(c => c.AnnualInterestRate)
                .GreaterThanOrEqualTo(0m).WithMessage(AppMessages.LoanNegativeRate);

            RuleFor(c => c.TermInMonths)
                .Must(AmortizationCalculator.IsAllowedTerm)
                .WithMessage(AppMessages.LoanInvalidTerm);
        }
    }

    public class CreateLoanCommandHandler : IRequestHandler<CreateLoanCommand, LoanDetailDto>
    {
        private readonly ILoanService _service;

        public CreateLoanCommandHandler(ILoanService service) => _service = service;

        public Task<LoanDetailDto> Handle(CreateLoanCommand request, CancellationToken cancellationToken)
            => _service.CreateAsync(new CreateLoanDto
            {
                ClientId = request.ClientId,
                CapitalAmount = request.CapitalAmount,
                TermInMonths = request.TermInMonths,
                AnnualInterestRate = request.AnnualInterestRate,
                ConfirmHighRisk = request.ConfirmHighRisk
            }, request.AdminUserId, cancellationToken);
    }
}
