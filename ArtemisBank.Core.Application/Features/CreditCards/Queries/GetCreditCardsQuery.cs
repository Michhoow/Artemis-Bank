using ArtemisBank.Core.Application.Common.Models;
using ArtemisBank.Core.Application.Dtos.CreditCards;
using ArtemisBank.Core.Application.Interfaces;
using FluentValidation;
using MediatR;

namespace ArtemisBank.Core.Application.Features.CreditCards.Queries
{
    public class GetCreditCardsQuery : IRequest<PagedResult<CreditCardDto>>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? Identification { get; set; }

        public string? Status { get; set; }
    }

    public class GetCreditCardsQueryValidator : AbstractValidator<GetCreditCardsQuery>
    {
        private static readonly string[] AllowedStatus = { "activa", "cancelada", "todas" };

        public GetCreditCardsQueryValidator()
        {
            RuleFor(q => q.Page)
                .GreaterThan(0).WithMessage("El parámetro page debe ser mayor que cero.");

            RuleFor(q => q.PageSize)
                .GreaterThan(0).WithMessage("El parámetro pageSize debe ser mayor que cero.")
                .LessThanOrEqualTo(PagedResult<CreditCardDto>.MaxPageSize)
                .WithMessage("El valor máximo permitido para pageSize es 20.");

            RuleFor(q => q.Status)
                .Must(s => s == null || AllowedStatus.Contains(s.Trim().ToLowerInvariant()))
                .WithMessage("El parámetro status solo admite los valores activa, cancelada o todas.");

            RuleFor(q => q.Identification)
                .Matches(@"^\d+$").When(q => !string.IsNullOrWhiteSpace(q.Identification))
                .WithMessage("La cédula solo puede contener dígitos.");
        }
    }

    public class GetCreditCardsQueryHandler
        : IRequestHandler<GetCreditCardsQuery, PagedResult<CreditCardDto>>
    {
        private readonly ICreditCardService _service;

        public GetCreditCardsQueryHandler(ICreditCardService service) => _service = service;

        public Task<PagedResult<CreditCardDto>> Handle(GetCreditCardsQuery request,
            CancellationToken cancellationToken)
            => _service.GetPagedAsync(new CreditCardFilterDto
            {
                Page = request.Page,
                PageSize = request.PageSize,
                Identification = request.Identification,
                Status = request.Status
            }, cancellationToken);
    }
}
