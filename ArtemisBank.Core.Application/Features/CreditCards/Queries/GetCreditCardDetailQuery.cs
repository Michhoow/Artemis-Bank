using ArtemisBank.Core.Application.Dtos.CreditCards;
using ArtemisBank.Core.Application.Interfaces;
using FluentValidation;
using MediatR;

namespace ArtemisBank.Core.Application.Features.CreditCards.Queries
{
    /// <summary>GET /api/credit-card/{id} — detalle de una tarjeta con su historial de consumos.</summary>
    public class GetCreditCardDetailQuery : IRequest<CreditCardDetailDto?>
    {
        public int Id { get; set; }
    }

    public class GetCreditCardDetailQueryValidator : AbstractValidator<GetCreditCardDetailQuery>
    {
        public GetCreditCardDetailQueryValidator()
        {
            RuleFor(q => q.Id).GreaterThan(0).WithMessage("El identificador de la tarjeta no es válido.");
        }
    }

    public class GetCreditCardDetailQueryHandler
        : IRequestHandler<GetCreditCardDetailQuery, CreditCardDetailDto?>
    {
        private readonly ICreditCardService _service;

        public GetCreditCardDetailQueryHandler(ICreditCardService service) => _service = service;

        public Task<CreditCardDetailDto?> Handle(GetCreditCardDetailQuery request,
            CancellationToken cancellationToken)
            => _service.GetDetailAsync(request.Id, cancellationToken);
    }
}
