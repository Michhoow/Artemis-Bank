using ArtemisBank.Core.Application.Common.Constants;
using ArtemisBank.Core.Application.Common.Exceptions;
using ArtemisBank.Core.Application.Dtos.CreditCards;
using ArtemisBank.Core.Application.Interfaces;
using FluentValidation;
using MediatR;

namespace ArtemisBank.Core.Application.Features.CreditCards.Queries
{
    public class GetCreditCardByIdQuery : IRequest<CreditCardDetailDto>
    {
        public int Id { get; set; }
    }

    public class GetCreditCardByIdQueryValidator : AbstractValidator<GetCreditCardByIdQuery>
    {
        public GetCreditCardByIdQueryValidator()
        {
            RuleFor(q => q.Id)
                .GreaterThan(0).WithMessage("El identificador de la tarjeta debe ser mayor que cero.");
        }
    }

    public class GetCreditCardByIdQueryHandler
        : IRequestHandler<GetCreditCardByIdQuery, CreditCardDetailDto>
    {
        private readonly ICreditCardService _service;

        public GetCreditCardByIdQueryHandler(ICreditCardService service) => _service = service;

        public async Task<CreditCardDetailDto> Handle(GetCreditCardByIdQuery request,
            CancellationToken cancellationToken)
            => await _service.GetDetailAsync(request.Id, cancellationToken)
               ?? throw new NotFoundException(AppMessages.CardNotFound);
    }
}
