using ArtemisBank.Core.Application.Common.Constants;
using ArtemisBank.Core.Application.Dtos.CreditCards;
using ArtemisBank.Core.Application.Interfaces;
using FluentValidation;
using MediatR;

namespace ArtemisBank.Core.Application.Features.CreditCards.Commands
{
    public class CashAdvanceCommand : IRequest<CashAdvanceResultDto>
    {
        public int CreditCardId { get; set; }
        public string TargetAccountNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }

        public string ClientId { get; set; } = string.Empty;
    }

    public class CashAdvanceCommandValidator : AbstractValidator<CashAdvanceCommand>
    {
        public CashAdvanceCommandValidator()
        {
            RuleFor(c => c.CreditCardId)
                .GreaterThan(0).WithMessage("Debe seleccionar una tarjeta de crédito válida.");

            RuleFor(c => c.TargetAccountNumber)
                .NotEmpty().WithMessage(AppMessages.AdvanceTargetAccountRequired)
                .Length(9).WithMessage("El número de cuenta debe contener 9 dígitos.")
                .Matches(@"^\d{9}$").WithMessage("El número de cuenta solo puede contener dígitos.");

            RuleFor(c => c.Amount)
                .GreaterThan(0m).WithMessage(AppMessages.AdvanceAmountGreaterThanZero);

            RuleFor(c => c.ClientId)
                .NotEmpty().WithMessage("No fue posible identificar al cliente autenticado.");
        }
    }

    public class CashAdvanceCommandHandler : IRequestHandler<CashAdvanceCommand, CashAdvanceResultDto>
    {
        private readonly ICreditCardService _service;

        public CashAdvanceCommandHandler(ICreditCardService service) => _service = service;

        public Task<CashAdvanceResultDto> Handle(CashAdvanceCommand request,
            CancellationToken cancellationToken)
            => _service.CashAdvanceAsync(new CashAdvanceRequestDto
            {
                CreditCardId = request.CreditCardId,
                TargetAccountNumber = request.TargetAccountNumber,
                Amount = request.Amount
            }, request.ClientId, cancellationToken);
    }
}
