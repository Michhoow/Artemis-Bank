using ArtemisBank.Core.Application.Common.Models;
using ArtemisBank.Core.Application.Dtos.CreditCards;
using ArtemisBank.Core.Application.Interfaces;
using FluentValidation;
using MediatR;

namespace ArtemisBank.Core.Application.Features.HermesPay.Queries
{
    /// <summary>
    /// GET /pay/get-transactions/{commerceId} — transacciones aprobadas recibidas por un comercio.
    /// El commerceId efectivo lo resuelve el controlador segun el rol.
    /// </summary>
    public class GetCommerceTransactionsQuery : IRequest<PagedResult<CommerceTransactionDto>>
    {
        public int CommerceId { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class GetCommerceTransactionsQueryValidator : AbstractValidator<GetCommerceTransactionsQuery>
    {
        public GetCommerceTransactionsQueryValidator()
        {
            RuleFor(q => q.CommerceId).GreaterThan(0).WithMessage("El identificador del comercio no es válido.");

            RuleFor(q => q.Page)
                .GreaterThan(0).WithMessage("El parámetro page debe ser mayor que cero.");

            RuleFor(q => q.PageSize)
                .GreaterThan(0).WithMessage("El parámetro pageSize debe ser mayor que cero.")
                .LessThanOrEqualTo(PagedResult<CommerceTransactionDto>.MaxPageSize)
                .WithMessage("El valor máximo permitido para pageSize es 20.");
        }
    }

    public class GetCommerceTransactionsQueryHandler
        : IRequestHandler<GetCommerceTransactionsQuery, PagedResult<CommerceTransactionDto>>
    {
        private readonly IHermesPayService _service;

        public GetCommerceTransactionsQueryHandler(IHermesPayService service) => _service = service;

        public Task<PagedResult<CommerceTransactionDto>> Handle(GetCommerceTransactionsQuery request,
            CancellationToken cancellationToken)
            => _service.GetTransactionsAsync(request.CommerceId, request.Page, request.PageSize, cancellationToken);
    }
}
