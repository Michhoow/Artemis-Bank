using ArtemisBank.Core.Application.Common.Constants;
using ArtemisBank.Core.Application.Interfaces;
using FluentValidation;
using MediatR;

namespace ArtemisBank.Core.Application.Features.Loans.Commands
{
    /// <summary>PATCH /api/loan/{id}/rate — modifica la tasa y recalcula solo cuotas futuras. Devuelve 204.</summary>
    public class UpdateLoanRateCommand : IRequest<Unit>
    {
        public int Id { get; set; }
        public decimal AnnualInterestRate { get; set; }
    }

    public class UpdateLoanRateCommandValidator : AbstractValidator<UpdateLoanRateCommand>
    {
        public UpdateLoanRateCommandValidator()
        {
            RuleFor(c => c.Id).GreaterThan(0).WithMessage("El identificador del préstamo no es válido.");

            RuleFor(c => c.AnnualInterestRate)
                .GreaterThanOrEqualTo(0m).WithMessage(AppMessages.NegativeInterestRate);
        }
    }

    public class UpdateLoanRateCommandHandler : IRequestHandler<UpdateLoanRateCommand, Unit>
    {
        private readonly ILoanService _service;

        public UpdateLoanRateCommandHandler(ILoanService service) => _service = service;

        public async Task<Unit> Handle(UpdateLoanRateCommand request, CancellationToken cancellationToken)
        {
            await _service.UpdateRateAsync(request.Id, request.AnnualInterestRate, cancellationToken);
            return Unit.Value;
        }
    }
}
