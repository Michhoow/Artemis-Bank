using ArtemisBank.Core.Application.Common.Models;
using ArtemisBank.Core.Application.Dtos.Loans;
using ArtemisBank.Core.Application.Interfaces;
using FluentValidation;
using MediatR;

namespace ArtemisBank.Core.Application.Features.Loans.Queries
{
    /// <summary>GET /api/loan — listado paginado de prestamos con filtros.</summary>
    public class GetLoansQuery : IRequest<PagedResult<LoanListItemDto>>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        /// <summary>Cedula del cliente.</summary>
        public string? Identification { get; set; }
        /// <summary>activos | completados | todos.</summary>
        public string? Status { get; set; }
    }

    public class GetLoansQueryValidator : AbstractValidator<GetLoansQuery>
    {
        private static readonly string[] AllowedStatus = { "activos", "completados", "todos" };

        public GetLoansQueryValidator()
        {
            RuleFor(q => q.Page)
                .GreaterThan(0).WithMessage("El parámetro page debe ser mayor que cero.");

            RuleFor(q => q.PageSize)
                .GreaterThan(0).WithMessage("El parámetro pageSize debe ser mayor que cero.")
                .LessThanOrEqualTo(PagedResult<LoanListItemDto>.MaxPageSize)
                .WithMessage("El valor máximo permitido para pageSize es 20.");

            RuleFor(q => q.Status)
                .Must(s => s == null || AllowedStatus.Contains(s.Trim().ToLowerInvariant()))
                .WithMessage("El parámetro status solo admite los valores activos, completados o todos.");
        }
    }

    public class GetLoansQueryHandler : IRequestHandler<GetLoansQuery, PagedResult<LoanListItemDto>>
    {
        private readonly ILoanService _service;

        public GetLoansQueryHandler(ILoanService service) => _service = service;

        public Task<PagedResult<LoanListItemDto>> Handle(GetLoansQuery request, CancellationToken cancellationToken)
            => _service.GetPagedAsync(new LoanFilterDto
            {
                Page = request.Page,
                PageSize = request.PageSize,
                Identification = request.Identification,
                Status = request.Status
            }, cancellationToken);
    }
}
