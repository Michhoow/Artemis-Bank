using ArtemisBank.Core.Application.Common.Models;
using ArtemisBank.Core.Application.Dtos.SavingsAccounts;
using ArtemisBank.Core.Application.Dtos.Transactions;
using ArtemisBank.Core.Application.Interfaces;
using FluentValidation;
using MediatR;

namespace ArtemisBank.Core.Application.Features.SavingsAccounts.Queries
{
    /// <summary>GET /api/savings-account/{accountNumber}/transactions — historial paginado.</summary>
    public class GetAccountTransactionsQuery : IRequest<SavingsAccountTransactionsDto>
    {
        public string AccountNumber { get; set; } = string.Empty;
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class GetAccountTransactionsQueryValidator : AbstractValidator<GetAccountTransactionsQuery>
    {
        public GetAccountTransactionsQueryValidator()
        {
            RuleFor(q => q.AccountNumber)
                .NotEmpty().WithMessage("El número de cuenta es requerido.")
                .Matches("^[0-9]{9}$").WithMessage("El número de cuenta debe contener exactamente 9 dígitos.");

            RuleFor(q => q.Page)
                .GreaterThan(0).WithMessage("El parámetro page debe ser mayor que cero.");

            RuleFor(q => q.PageSize)
                .GreaterThan(0).WithMessage("El parámetro pageSize debe ser mayor que cero.")
                .LessThanOrEqualTo(PagedResult<TransactionDto>.MaxPageSize)
                .WithMessage("El valor máximo permitido para pageSize es 20.");
        }
    }

    public class GetAccountTransactionsQueryHandler
        : IRequestHandler<GetAccountTransactionsQuery, SavingsAccountTransactionsDto>
    {
        private readonly ISavingsAccountService _service;

        public GetAccountTransactionsQueryHandler(ISavingsAccountService service) => _service = service;

        public Task<SavingsAccountTransactionsDto> Handle(GetAccountTransactionsQuery request,
            CancellationToken cancellationToken)
            => _service.GetTransactionsAsync(request.AccountNumber, request.Page, request.PageSize, cancellationToken);
    }
}
