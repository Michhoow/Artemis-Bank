using ArtemisBank.Core.Application.Common.Constants;
using ArtemisBank.Core.Application.Dtos.SavingsAccounts;
using ArtemisBank.Core.Application.Interfaces;
using FluentValidation;
using MediatR;

namespace ArtemisBank.Core.Application.Features.SavingsAccounts.Commands
{
    public class CreateSecondarySavingsAccountCommand : IRequest<SavingsAccountDto>
    {
        public string ClientId { get; set; } = string.Empty;
        public decimal InitialBalance { get; set; }

        public string? CreatedByUserId { get; set; }
    }

    public class CreateSecondarySavingsAccountCommandValidator
        : AbstractValidator<CreateSecondarySavingsAccountCommand>
    {
        public CreateSecondarySavingsAccountCommandValidator()
        {
            RuleFor(c => c.ClientId)
                .NotEmpty().WithMessage(AppMessages.MustSelectClient);

            RuleFor(c => c.InitialBalance)
                .GreaterThanOrEqualTo(0m).WithMessage(AppMessages.NegativeInitialBalance);
        }
    }

    public class CreateSecondarySavingsAccountCommandHandler
        : IRequestHandler<CreateSecondarySavingsAccountCommand, SavingsAccountDto>
    {
        private readonly ISavingsAccountService _service;

        public CreateSecondarySavingsAccountCommandHandler(ISavingsAccountService service) => _service = service;

        public Task<SavingsAccountDto> Handle(CreateSecondarySavingsAccountCommand request,
            CancellationToken cancellationToken)
            => _service.CreateSecondaryAsync(request.ClientId, request.InitialBalance,
                request.CreatedByUserId, cancellationToken);
    }
}
