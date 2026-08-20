using ArtemisBank.Core.Application.Interfaces;
using FluentValidation;
using MediatR;

namespace ArtemisBank.Core.Application.Features.CreditCards.Commands
{
    /// <summary>PATCH /api/credit-card/{id}/cancel — cancela una tarjeta activa sin deuda. Devuelve 204.</summary>
    public class CancelCardCommand : IRequest<Unit>
    {
        public int Id { get; set; }
    }

    public class CancelCardCommandValidator : AbstractValidator<CancelCardCommand>
    {
        public CancelCardCommandValidator()
        {
            RuleFor(c => c.Id).GreaterThan(0).WithMessage("El identificador de la tarjeta no es válido.");
        }
    }

    public class CancelCardCommandHandler : IRequestHandler<CancelCardCommand, Unit>
    {
        private readonly ICreditCardService _service;

        public CancelCardCommandHandler(ICreditCardService service) => _service = service;

        public async Task<Unit> Handle(CancelCardCommand request, CancellationToken cancellationToken)
        {
            await _service.CancelAsync(request.Id, cancellationToken);
            return Unit.Value;
        }
    }
}
