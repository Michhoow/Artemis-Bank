using ArtemisBank.Core.Application.Common.Constants;
using ArtemisBank.Core.Application.Dtos.HermesPay;
using ArtemisBank.Core.Application.Interfaces;
using FluentValidation;
using MediatR;

namespace ArtemisBank.Core.Application.Features.HermesPay.Commands
{
    public class ProcessPaymentCommand : IRequest<HermesPayResultDto>
    {
        public string CardNumber { get; set; } = string.Empty;
        public string MonthExpirationCard { get; set; } = string.Empty;
        public string YearExpirationCard { get; set; } = string.Empty;
        public string Cvc { get; set; } = string.Empty;
        public decimal TransactionAmount { get; set; }

        public int CommerceId { get; set; }
        public string? PerformedByUserId { get; set; }
    }

    public class ProcessPaymentCommandValidator : AbstractValidator<ProcessPaymentCommand>
    {
        public ProcessPaymentCommandValidator()
        {
            RuleFor(c => c.CardNumber)
                .NotEmpty().WithMessage("El número de tarjeta es requerido.")
                .Matches(@"^\d{16}$").WithMessage(AppMessages.HermesInvalidCardNumber);

            RuleFor(c => c.MonthExpirationCard)
                .NotEmpty().WithMessage("El mes de expiración es requerido.")
                .Matches(@"^(0[1-9]|1[0-2])$").WithMessage(AppMessages.HermesInvalidExpirationMonth);

            RuleFor(c => c.YearExpirationCard)
                .NotEmpty().WithMessage("El año de expiración es requerido.")
                .Matches(@"^\d{2}$|^\d{4}$").WithMessage(AppMessages.HermesInvalidExpirationYear);

            RuleFor(c => c.Cvc)
                .NotEmpty().WithMessage("El código de seguridad es requerido.")
                .Matches(@"^\d{3}$").WithMessage("El código de seguridad debe contener 3 dígitos.");

            RuleFor(c => c.TransactionAmount)
                .GreaterThan(0m).WithMessage(AppMessages.HermesAmountGreaterThanZero);

            RuleFor(c => c.CommerceId)
                .GreaterThan(0).WithMessage("El identificador del comercio debe ser mayor que cero.");
        }
    }

    public class ProcessPaymentCommandHandler : IRequestHandler<ProcessPaymentCommand, HermesPayResultDto>
    {
        private readonly IHermesPayService _service;

        public ProcessPaymentCommandHandler(IHermesPayService service) => _service = service;

        public Task<HermesPayResultDto> Handle(ProcessPaymentCommand request,
            CancellationToken cancellationToken)
            => _service.ProcessPaymentAsync(new HermesPayRequestDto
            {
                CardNumber = request.CardNumber,
                MonthExpirationCard = request.MonthExpirationCard,
                YearExpirationCard = request.YearExpirationCard,
                Cvc = request.Cvc,
                TransactionAmount = request.TransactionAmount
            }, request.CommerceId, request.PerformedByUserId, cancellationToken);
    }
}
