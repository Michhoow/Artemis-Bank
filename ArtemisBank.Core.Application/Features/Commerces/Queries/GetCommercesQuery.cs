using ArtemisBank.Core.Application.Common.Constants;
using ArtemisBank.Core.Application.Common.Exceptions;
using ArtemisBank.Core.Application.Common.Models;
using ArtemisBank.Core.Application.Dtos.Commerces;
using ArtemisBank.Core.Application.Interfaces;
using FluentValidation;
using MediatR;

namespace ArtemisBank.Core.Application.Features.Commerces.Queries
{
    public class GetCommercesQuery : IRequest<PagedResult<CommerceDto>>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;

        public string? Status { get; set; }
    }

    public class GetCommercesQueryValidator : AbstractValidator<GetCommercesQuery>
    {
        public GetCommercesQueryValidator()
        {
            RuleFor(q => q.Page)
                .GreaterThan(0).WithMessage("El parámetro page debe ser mayor que cero.");

            RuleFor(q => q.PageSize)
                .GreaterThan(0).WithMessage("El parámetro pageSize debe ser mayor que cero.")
                .LessThanOrEqualTo(PagedResult<CommerceDto>.MaxPageSize)
                .WithMessage("El valor máximo permitido para pageSize es 20.");

            RuleFor(q => q.Status)
                .Must(s => s == null || AllowedStatus.Contains(s.Trim().ToLowerInvariant()))
                .WithMessage("El parámetro status solo admite los valores activo, inactivo o todos.");
        }

        private static readonly string[] AllowedStatus = { "activo", "inactivo", "todos" };
    }

    public class GetCommercesQueryHandler : IRequestHandler<GetCommercesQuery, PagedResult<CommerceDto>>
    {
        private readonly ICommerceService _service;

        public GetCommercesQueryHandler(ICommerceService service) => _service = service;

        public Task<PagedResult<CommerceDto>> Handle(GetCommercesQuery request,
            CancellationToken cancellationToken)
            => _service.GetPagedAsync(request.Page, request.PageSize, request.Status, cancellationToken);
    }

    public class GetCommerceByIdQuery : IRequest<CommerceDetailDto>
    {
        public int Id { get; set; }
    }

    public class GetCommerceByIdQueryValidator : AbstractValidator<GetCommerceByIdQuery>
    {
        public GetCommerceByIdQueryValidator()
        {
            RuleFor(q => q.Id)
                .GreaterThan(0).WithMessage("El identificador del comercio debe ser mayor que cero.");
        }
    }

    public class GetCommerceByIdQueryHandler : IRequestHandler<GetCommerceByIdQuery, CommerceDetailDto>
    {
        private readonly ICommerceService _service;

        public GetCommerceByIdQueryHandler(ICommerceService service) => _service = service;

        public async Task<CommerceDetailDto> Handle(GetCommerceByIdQuery request,
            CancellationToken cancellationToken)
            => await _service.GetDetailAsync(request.Id, cancellationToken)
               ?? throw new NotFoundException(AppMessages.CommerceNotFound);
    }
}
