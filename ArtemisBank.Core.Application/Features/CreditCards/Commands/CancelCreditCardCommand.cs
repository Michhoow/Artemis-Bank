using ArtemisBank.Core.Application.Interfaces;
using FluentValidation;
using MediatR;

namespace ArtemisBank.Core.Application.Features.CreditCards.Commands
{
    public class CancelCreditCardCommand : IRequest<Unit>
    {
        public int CreditCardId { get; set; }
        public string? AdminUserId { get; set; }
    }

    public class CancelCreditCardCommandValidator : AbstractValidator<CancelCreditCardCommand>
    {
        public CancelCreditCardCommandValidator()
        {
            RuleFor(c => c.CreditCardId)
                .GreaterThan(0).WithMessage("El identificador de la tarjeta debe ser mayor que cero.");
        }
    }

    public class CancelCreditCardCommandHandler : IRequestHandler<CancelCreditCardCommand, Unit>
    {
        private readonly ICreditCardService _service;

        public CancelCreditCardCommandHandler(ICreditCardService service) => _service = service;

        public async Task<Unit> Handle(CancelCreditCardCommand request, CancellationToken cancellationToken)
        {
            await _service.CancelAsync(request.CreditCardId, request.AdminUserId, cancellationToken);
            return Unit.Value;
        }
    }
}
