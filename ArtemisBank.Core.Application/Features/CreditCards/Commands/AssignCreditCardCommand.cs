using ArtemisBank.Core.Application.Common.Constants;
using ArtemisBank.Core.Application.Dtos.CreditCards;
using ArtemisBank.Core.Application.Interfaces;
using FluentValidation;
using MediatR;

namespace ArtemisBank.Core.Application.Features.CreditCards.Commands
{
    /// <summary>POST /api/credit-card — emite una tarjeta de credito a un cliente activo.</summary>
    public class AssignCreditCardCommand : IRequest<CreditCardListItemDto>
    {
        public string ClientId { get; set; } = string.Empty;
        public decimal CreditLimit { get; set; }
        /// <summary>Administrador autenticado (se toma del JWT).</summary>
        public string? AssignedByUserId { get; set; }
    }

    public class AssignCreditCardCommandValidator : AbstractValidator<AssignCreditCardCommand>
    {
        public AssignCreditCardCommandValidator()
        {
            RuleFor(c => c.ClientId)
                .NotEmpty().WithMessage(AppMessages.MustSelectClient);

            RuleFor(c => c.CreditLimit)
                .GreaterThan(0m).WithMessage(AppMessages.CreditLimitGreaterThanZero);
        }
    }

    public class AssignCreditCardCommandHandler
        : IRequestHandler<AssignCreditCardCommand, CreditCardListItemDto>
    {
        private readonly ICreditCardService _service;

        public AssignCreditCardCommandHandler(ICreditCardService service) => _service = service;

        public Task<CreditCardListItemDto> Handle(AssignCreditCardCommand request,
            CancellationToken cancellationToken)
            => _service.AssignAsync(new AssignCreditCardDto
            {
                ClientId = request.ClientId,
                CreditLimit = request.CreditLimit,
                AssignedByUserId = request.AssignedByUserId
            }, cancellationToken);
    }
}
