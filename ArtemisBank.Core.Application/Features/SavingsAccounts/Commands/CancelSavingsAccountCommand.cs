using ArtemisBank.Core.Application.Interfaces;
using FluentValidation;
using MediatR;

namespace ArtemisBank.Core.Application.Features.SavingsAccounts.Commands
{
    /// <summary>
    /// PATCH /api/savings-account/{accountNumber}/cancel — cancela una cuenta secundaria activa.
    /// Si tiene balance, lo traslada a la principal con registro cruzado antes de cancelar.
    /// </summary>
    public class CancelSavingsAccountCommand : IRequest<Unit>
    {
        public string AccountNumber { get; set; } = string.Empty;
        public string? CancelledByUserId { get; set; }
    }

    public class CancelSavingsAccountCommandValidator : AbstractValidator<CancelSavingsAccountCommand>
    {
        public CancelSavingsAccountCommandValidator()
        {
            RuleFor(c => c.AccountNumber)
                .NotEmpty().WithMessage("El número de cuenta es requerido.")
                .Matches("^[0-9]{9}$").WithMessage("El número de cuenta debe contener exactamente 9 dígitos.");
        }
    }

    public class CancelSavingsAccountCommandHandler : IRequestHandler<CancelSavingsAccountCommand, Unit>
    {
        private readonly ISavingsAccountService _service;

        public CancelSavingsAccountCommandHandler(ISavingsAccountService service) => _service = service;

        public async Task<Unit> Handle(CancelSavingsAccountCommand request, CancellationToken cancellationToken)
        {
            await _service.CancelSecondaryAsync(request.AccountNumber, request.CancelledByUserId, cancellationToken);
            return Unit.Value;
        }
    }
}
