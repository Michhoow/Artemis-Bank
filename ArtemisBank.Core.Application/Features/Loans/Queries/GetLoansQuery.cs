using ArtemisBank.Core.Application.Common.Models;
using ArtemisBank.Core.Application.Dtos.Loans;
using ArtemisBank.Core.Application.Interfaces;
using FluentValidation;
using MediatR;

namespace ArtemisBank.Core.Application.Features.Loans.Queries
{
    public class GetLoansQuery : IRequest<PagedResult<LoanDto>>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;

        public string? Identification { get; set; }

        public string? Status { get; set; }
    }

    public class GetLoansQueryValidator : AbstractValidator<GetLoansQuery>
    {
        private static readonly string[] AllowedStatus = { "activo", "completado", "todos" };

        public GetLoansQueryValidator()
        {
            RuleFor(q => q.Page)
                .GreaterThan(0).WithMessage("El parámetro page debe ser mayor que cero.");

            RuleFor(q => q.PageSize)
                .GreaterThan(0).WithMessage("El parámetro pageSize debe ser mayor que cero.")
                .LessThanOrEqualTo(PagedResult<LoanDto>.MaxPageSize)
                .WithMessage("El valor máximo permitido para pageSize es 20.");

            RuleFor(q => q.Status)
                .Must(s => s == null || AllowedStatus.Contains(s.Trim().ToLowerInvariant()))
                .WithMessage("El parámetro status solo admite los valores activo, completado o todos.");

            RuleFor(q => q.Identification)
                .Matches(@"^\d+$").When(q => !string.IsNullOrWhiteSpace(q.Identification))
                .WithMessage("La cédula solo puede contener dígitos.");
        }
    }

    public class GetLoansQueryHandler : IRequestHandler<GetLoansQuery, PagedResult<LoanDto>>
    {
        private readonly ILoanService _service;

        public GetLoansQueryHandler(ILoanService service) => _service = service;

        public Task<PagedResult<LoanDto>> Handle(GetLoansQuery request, CancellationToken cancellationToken)
            => _service.GetPagedAsync(new LoanFilterDto
            {
                Page = request.Page,
                PageSize = request.PageSize,
                Identification = request.Identification,
                Status = request.Status
            }, cancellationToken);
    }
}
