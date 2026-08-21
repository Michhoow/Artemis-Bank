using ArtemisBank.Core.Application.Common.Models;
using ArtemisBank.Core.Application.Dtos.Common;
using ArtemisBank.Core.Application.Interfaces.Contracts;
using FluentValidation;
using MediatR;

namespace ArtemisBank.Core.Application.Features.Users.Queries
{
    public class GetUsersQuery : IRequest<PagedResult<UserInfoDto>>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;

        public string? Role { get; set; }
    }

    public class GetUsersQueryValidator : AbstractValidator<GetUsersQuery>
    {
        private static readonly string[] AllowedRoles =
            { "administrador", "cajero", "cliente", "comercio" };

        public GetUsersQueryValidator()
        {
            RuleFor(q => q.Page)
                .GreaterThan(0).WithMessage("El parámetro page debe ser mayor que cero.");

            RuleFor(q => q.PageSize)
                .GreaterThan(0).WithMessage("El parámetro pageSize debe ser mayor que cero.")
                .LessThanOrEqualTo(PagedResult<UserInfoDto>.MaxPageSize)
                .WithMessage("El valor máximo permitido para pageSize es 20.");

            RuleFor(q => q.Role)
                .Must(r => r == null || AllowedRoles.Contains(r.Trim().ToLowerInvariant()))
                .WithMessage("El parámetro role solo admite Administrador, Cajero, Cliente o Comercio.");
        }
    }

    public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, PagedResult<UserInfoDto>>
    {
        private readonly IUserManagementService _service;

        public GetUsersQueryHandler(IUserManagementService service) => _service = service;

        public Task<PagedResult<UserInfoDto>> Handle(GetUsersQuery request,
            CancellationToken cancellationToken)
            => _service.GetPagedAsync(request.Page, request.PageSize, request.Role, cancellationToken);
    }
}
