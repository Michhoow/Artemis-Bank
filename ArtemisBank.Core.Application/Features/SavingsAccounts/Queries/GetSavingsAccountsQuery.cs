using ArtemisBank.Core.Application.Common.Models;
using ArtemisBank.Core.Application.Dtos.SavingsAccounts;
using ArtemisBank.Core.Application.Interfaces;
using FluentValidation;
using MediatR;

namespace ArtemisBank.Core.Application.Features.SavingsAccounts.Queries
{
    /// <summary>GET /api/savings-account — listado paginado con filtros.</summary>
    public class GetSavingsAccountsQuery : IRequest<PagedResult<SavingsAccountDto>>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        /// <summary>Cedula del cliente.</summary>
        public string? Identification { get; set; }
        /// <summary>activa | cancelada | todas.</summary>
        public string? Status { get; set; }
        /// <summary>principal | secundaria | todas.</summary>
        public string? Type { get; set; }
    }

    /// <summary>Validaciones estructurales de los query params.</summary>
    public class GetSavingsAccountsQueryValidator : AbstractValidator<GetSavingsAccountsQuery>
    {
        private static readonly string[] AllowedStatus = { "activa", "cancelada", "todas" };
        private static readonly string[] AllowedTypes = { "principal", "secundaria", "todas" };

        public GetSavingsAccountsQueryValidator()
        {
            RuleFor(q => q.Page)
                .GreaterThan(0).WithMessage("El parámetro page debe ser mayor que cero.");

            RuleFor(q => q.PageSize)
                .GreaterThan(0).WithMessage("El parámetro pageSize debe ser mayor que cero.")
                .LessThanOrEqualTo(PagedResult<SavingsAccountDto>.MaxPageSize)
                .WithMessage("El valor máximo permitido para pageSize es 20.");

            RuleFor(q => q.Status)
                .Must(s => s == null || AllowedStatus.Contains(s.Trim().ToLowerInvariant()))
                .WithMessage("El parámetro status solo admite los valores activa, cancelada o todas.");

            RuleFor(q => q.Type)
                .Must(t => t == null || AllowedTypes.Contains(t.Trim().ToLowerInvariant()))
                .WithMessage("El parámetro type solo admite los valores principal, secundaria o todas.");
        }
    }

    public class GetSavingsAccountsQueryHandler
        : IRequestHandler<GetSavingsAccountsQuery, PagedResult<SavingsAccountDto>>
    {
        private readonly ISavingsAccountService _service;

        public GetSavingsAccountsQueryHandler(ISavingsAccountService service) => _service = service;

        public Task<PagedResult<SavingsAccountDto>> Handle(GetSavingsAccountsQuery request,
            CancellationToken cancellationToken)
            => _service.GetPagedAsync(new SavingsAccountFilterDto
            {
                Page = request.Page,
                PageSize = request.PageSize,
                Identification = request.Identification,
                Status = request.Status,
                Type = request.Type
            }, cancellationToken);
    }
}
