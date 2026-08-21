using ArtemisBank.Core.Application.Common.Constants;
using ArtemisBank.Core.Application.Dtos.CreditCards;
using ArtemisBank.Core.Application.Interfaces;
using FluentValidation;
using MediatR;

namespace ArtemisBank.Core.Application.Features.CreditCards.Commands
{
    public class UpdateCardLimitCommand : IRequest<CreditCardDto>
    {
        public int CreditCardId { get; set; }
        public decimal CreditLimit { get; set; }
        public string? AdminUserId { get; set; }
    }

    public class UpdateCardLimitCommandValidator : AbstractValidator<UpdateCardLimitCommand>
    {
        public UpdateCardLimitCommandValidator()
        {
            RuleFor(c => c.CreditCardId)
                .GreaterThan(0).WithMessage("El identificador de la tarjeta debe ser mayor que cero.");

            RuleFor(c => c.CreditLimit)
                .GreaterThan(0m).WithMessage(AppMessages.CardLimitGreaterThanZero);
        }
    }

    public class UpdateCardLimitCommandHandler : IRequestHandler<UpdateCardLimitCommand, CreditCardDto>
    {
        private readonly ICreditCardService _service;

        public UpdateCardLimitCommandHandler(ICreditCardService service) => _service = service;

        public Task<CreditCardDto> Handle(UpdateCardLimitCommand request, CancellationToken cancellationToken)
            => _service.UpdateLimitAsync(request.CreditCardId, request.CreditLimit,
                request.AdminUserId, cancellationToken);
    }
}
