using ArtemisBank.Core.Application.Common.Constants;
using ArtemisBank.Core.Application.Common.Models;
using ArtemisBank.Core.Application.Dtos.CreditCards;
using ArtemisBank.Core.Application.Interfaces;
using FluentValidation;
using MediatR;

namespace ArtemisBank.Core.Application.Features.HermesPay.Commands
{
    /// <summary>
    /// POST /pay/process-payment/{commerceId} — procesa un pago de tarjeta hacia un comercio.
    /// El commerceId efectivo lo resuelve el controlador segun el rol (URL para Administrador,
    /// JWT para Comercio) antes de construir el command.
    /// </summary>
    public class ProcessPaymentCommand : IRequest<OperationResult>
    {
        public int CommerceId { get; set; }
        public string CardNumber { get; set; } = string.Empty;
        public string MonthExpirationCard { get; set; } = string.Empty;
        public string YearExpirationCard { get; set; } = string.Empty;
        public string Cvc { get; set; } = string.Empty;
        public decimal TransactionAmount { get; set; }
        public string? AuthenticatedUserId { get; set; }
        public bool IsCommerceRole { get; set; }
    }

    public class ProcessPaymentCommandValidator : AbstractValidator<ProcessPaymentCommand>
    {
        public ProcessPaymentCommandValidator()
        {
            RuleFor(c => c.CommerceId)
                .GreaterThan(0).WithMessage(AppMessages.CommerceNotFound);

            RuleFor(c => c.CardNumber)
                .NotEmpty().WithMessage(AppMessages.InvalidCardData)
                .Length(16).WithMessage(AppMessages.InvalidCardData)
                .Matches("^[0-9]+$").WithMessage(AppMessages.InvalidCardData);

            RuleFor(c => c.MonthExpirationCard)
                .NotEmpty().WithMessage(AppMessages.InvalidCardData);

            RuleFor(c => c.YearExpirationCard)
                .NotEmpty().WithMessage(AppMessages.InvalidCardData);

            RuleFor(c => c.Cvc)
                .NotEmpty().WithMessage(AppMessages.InvalidCardData)
                .Length(3).WithMessage(AppMessages.InvalidCardData)
                .Matches("^[0-9]+$").WithMessage(AppMessages.InvalidCardData);

            RuleFor(c => c.TransactionAmount)
                .GreaterThan(0m).WithMessage(AppMessages.TransactionAmountGreaterThanZero);
        }
    }

    public class ProcessPaymentCommandHandler : IRequestHandler<ProcessPaymentCommand, OperationResult>
    {
        private readonly IHermesPayService _service;

        public ProcessPaymentCommandHandler(IHermesPayService service) => _service = service;

        public Task<OperationResult> Handle(ProcessPaymentCommand request, CancellationToken cancellationToken)
            => _service.ProcessPaymentAsync(new ProcessPaymentDto
            {
                CommerceId = request.CommerceId,
                CardNumber = request.CardNumber,
                MonthExpirationCard = request.MonthExpirationCard,
                YearExpirationCard = request.YearExpirationCard,
                Cvc = request.Cvc,
                TransactionAmount = request.TransactionAmount,
                AuthenticatedUserId = request.AuthenticatedUserId,
                IsCommerceRole = request.IsCommerceRole
            }, cancellationToken);
    }
}
