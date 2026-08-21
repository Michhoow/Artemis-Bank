using ArtemisBank.Core.Application.Common.Constants;
using ArtemisBank.Core.Application.Dtos.CreditCards;
using ArtemisBank.Core.Application.Interfaces;
using FluentValidation;
using MediatR;

namespace ArtemisBank.Core.Application.Features.CreditCards.Commands
{
    public class CreateCreditCardCommand : IRequest<CreatedCreditCardDto>
    {
        public string ClientId { get; set; } = string.Empty;
        public decimal CreditLimit { get; set; }
        public string? AdminUserId { get; set; }
    }

    public class CreateCreditCardCommandValidator : AbstractValidator<CreateCreditCardCommand>
    {
        public CreateCreditCardCommandValidator()
        {
            RuleFor(c => c.ClientId)
                .NotEmpty().WithMessage(AppMessages.CardClientRequired);

            RuleFor(c => c.CreditLimit)
                .GreaterThan(0m).WithMessage(AppMessages.CardLimitGreaterThanZero);
        }
    }

    public class CreateCreditCardCommandHandler
        : IRequestHandler<CreateCreditCardCommand, CreatedCreditCardDto>
    {
        private readonly ICreditCardService _service;

        public CreateCreditCardCommandHandler(ICreditCardService service) => _service = service;

        public Task<CreatedCreditCardDto> Handle(CreateCreditCardCommand request,
            CancellationToken cancellationToken)
            => _service.CreateAsync(new CreateCreditCardDto
            {
                ClientId = request.ClientId,
                CreditLimit = request.CreditLimit
            }, request.AdminUserId, cancellationToken);
    }
}
