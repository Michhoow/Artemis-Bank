using ArtemisBank.Core.Application.Dtos.HermesPay;
using ArtemisBank.Core.Application.Interfaces;
using FluentValidation;
using MediatR;

namespace ArtemisBank.Core.Application.Features.HermesPay.Queries
{
    public class GetCommerceTransactionsQuery : IRequest<CommerceTransactionsDto>
    {
        public int CommerceId { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class GetCommerceTransactionsQueryValidator : AbstractValidator<GetCommerceTransactionsQuery>
    {
        public GetCommerceTransactionsQueryValidator()
        {
            RuleFor(q => q.CommerceId)
                .GreaterThan(0).WithMessage("El identificador del comercio debe ser mayor que cero.");

            RuleFor(q => q.Page)
                .GreaterThan(0).WithMessage("El parámetro page debe ser mayor que cero.");

            RuleFor(q => q.PageSize)
                .GreaterThan(0).WithMessage("El parámetro pageSize debe ser mayor que cero.")
                .LessThanOrEqualTo(20).WithMessage("El valor máximo permitido para pageSize es 20.");
        }
    }

    public class GetCommerceTransactionsQueryHandler
        : IRequestHandler<GetCommerceTransactionsQuery, CommerceTransactionsDto>
    {
        private readonly IHermesPayService _service;

        public GetCommerceTransactionsQueryHandler(IHermesPayService service) => _service = service;

        public Task<CommerceTransactionsDto> Handle(GetCommerceTransactionsQuery request,
            CancellationToken cancellationToken)
            => _service.GetTransactionsAsync(request.CommerceId, request.Page, request.PageSize,
                cancellationToken);
    }
}
