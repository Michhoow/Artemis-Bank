using ArtemisBank.Core.Application.Common.Constants;
using ArtemisBank.Core.Application.Dtos.Loans;
using ArtemisBank.Core.Application.Interfaces;
using FluentValidation;
using MediatR;

namespace ArtemisBank.Core.Application.Features.Loans.Commands
{
    public class UpdateLoanRateCommand : IRequest<LoanDetailDto>
    {
        public int LoanId { get; set; }
        public decimal AnnualInterestRate { get; set; }
        public string? AdminUserId { get; set; }
    }

    public class UpdateLoanRateCommandValidator : AbstractValidator<UpdateLoanRateCommand>
    {
        public UpdateLoanRateCommandValidator()
        {
            RuleFor(c => c.LoanId)
                .GreaterThan(0).WithMessage("El identificador del préstamo debe ser mayor que cero.");

            RuleFor(c => c.AnnualInterestRate)
                .GreaterThanOrEqualTo(0m).WithMessage(AppMessages.LoanNegativeRate);
        }
    }

    public class UpdateLoanRateCommandHandler : IRequestHandler<UpdateLoanRateCommand, LoanDetailDto>
    {
        private readonly ILoanService _service;

        public UpdateLoanRateCommandHandler(ILoanService service) => _service = service;

        public Task<LoanDetailDto> Handle(UpdateLoanRateCommand request, CancellationToken cancellationToken)
            => _service.UpdateRateAsync(request.LoanId, request.AnnualInterestRate,
                request.AdminUserId, cancellationToken);
    }
}
