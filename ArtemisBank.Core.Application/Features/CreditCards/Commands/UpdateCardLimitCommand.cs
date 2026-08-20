using ArtemisBank.Core.Application.Common.Constants;
using ArtemisBank.Core.Application.Interfaces;
using FluentValidation;
using MediatR;

namespace ArtemisBank.Core.Application.Features.CreditCards.Commands
{
    /// <summary>PATCH /api/credit-card/{id}/limit — modifica el limite de credito. Devuelve 204.</summary>
    public class UpdateCardLimitCommand : IRequest<Unit>
    {
        public int Id { get; set; }
        public decimal CreditLimit { get; set; }
    }

    public class UpdateCardLimitCommandValidator : AbstractValidator<UpdateCardLimitCommand>
    {
        public UpdateCardLimitCommandValidator()
        {
            RuleFor(c => c.Id).GreaterThan(0).WithMessage("El identificador de la tarjeta no es válido.");

            RuleFor(c => c.CreditLimit)
                .GreaterThan(0m).WithMessage(AppMessages.CreditLimitGreaterThanZero);
        }
    }

    public class UpdateCardLimitCommandHandler : IRequestHandler<UpdateCardLimitCommand, Unit>
    {
        private readonly ICreditCardService _service;

        public UpdateCardLimitCommandHandler(ICreditCardService service) => _service = service;

        public async Task<Unit> Handle(UpdateCardLimitCommand request, CancellationToken cancellationToken)
        {
            await _service.UpdateLimitAsync(request.Id, request.CreditLimit, cancellationToken);
            return Unit.Value;
        }
    }
}
