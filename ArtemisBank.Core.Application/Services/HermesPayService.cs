using ArtemisBank.Core.Application.Common.Constants;
using ArtemisBank.Core.Application.Common.Exceptions;
using ArtemisBank.Core.Application.Common.Models;
using ArtemisBank.Core.Application.Dtos.CreditCards;
using ArtemisBank.Core.Application.Interfaces;
using ArtemisBank.Core.Application.Interfaces.Contracts;
using ArtemisBank.Core.Domain.Common.Enums;
using ArtemisBank.Core.Domain.Entities;
using ArtemisBank.Core.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ArtemisBank.Core.Application.Services
{
    /// <summary>
    /// Procesador de pago Hermes Pay. Dueno: Manuel.
    ///
    /// Cobra a una tarjeta de credito y acredita a la cuenta principal del comercio.
    /// La resolucion del comercio segun rol (URL para Administrador, JWT para Comercio) la hace el
    /// controlador; aqui llega ya el commerceId efectivo. La operacion es transaccional: consumo,
    /// deuda de la tarjeta y credito al comercio se aplican juntos o no se aplican.
    /// </summary>
    public class HermesPayService : IHermesPayService
    {
        private readonly ICreditCardRepository _creditCardRepository;
        private readonly ICommerceRepository _commerceRepository;
        private readonly ISavingsAccountService _savingsAccountService;
        private readonly ITransactionService _transactionService;
        private readonly IUserReadService _userReadService;
        private readonly ICryptoService _cryptoService;
        private readonly IEmailService _emailService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<HermesPayService> _logger;

        public HermesPayService(
            ICreditCardRepository creditCardRepository,
            ICommerceRepository commerceRepository,
            ISavingsAccountService savingsAccountService,
            ITransactionService transactionService,
            IUserReadService userReadService,
            ICryptoService cryptoService,
            IEmailService emailService,
            IUnitOfWork unitOfWork,
            ILogger<HermesPayService> logger)
        {
            _creditCardRepository = creditCardRepository;
            _commerceRepository = commerceRepository;
            _savingsAccountService = savingsAccountService;
            _transactionService = transactionService;
            _userReadService = userReadService;
            _cryptoService = cryptoService;
            _emailService = emailService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        // ================================================================== consultar transacciones

        public async Task<PagedResult<CommerceTransactionDto>> GetTransactionsAsync(int commerceId, int page,
            int pageSize, CancellationToken cancellationToken = default)
        {
            var normalizedPage = page <= 0 ? 1 : page;
            var normalizedSize = NormalizePageSize(pageSize);

            var commerce = await _commerceRepository.GetByIdAsync(commerceId)
                ?? throw new NotFoundException(AppMessages.CommerceNotFound);

            var query = _creditCardRepository.QueryConsumptionsByCommerce(commerce.Id)
                .Include(k => k.CreditCard)
                .OrderByDescending(k => k.CreatedAt);

            var total = await query.CountAsync(cancellationToken);
            var items = await query
                .Skip((normalizedPage - 1) * normalizedSize)
                .Take(normalizedSize)
                .ToListAsync(cancellationToken);

            var data = items.Select(k => new CommerceTransactionDto
            {
                Id = k.Id.ToString(),
                TransactionDate = k.CreatedAt,
                Amount = k.Amount,
                CardLastFourDigits = k.CreditCard?.LastFourDigits ?? string.Empty,
                Status = "APROBADO"
            }).ToList();

            return PagedResult<CommerceTransactionDto>.Create(data, normalizedPage, normalizedSize, total);
        }

        // ================================================================== procesar pago

        public async Task<OperationResult> ProcessPaymentAsync(ProcessPaymentDto request,
            CancellationToken cancellationToken = default)
        {
            var amount = Money.Round(request.TransactionAmount);
            if (amount <= 0m) return OperationResult.Failure(AppMessages.TransactionAmountGreaterThanZero);

            // ---- Comercio (ya resuelto por rol en el controlador).
            var commerce = await _commerceRepository.GetByIdAsync(request.CommerceId)
                ?? throw new NotFoundException(AppMessages.CommerceNotFound);

            if (!commerce.IsActive) return OperationResult.Failure(AppMessages.CommerceInactive);
            if (string.IsNullOrWhiteSpace(commerce.UserId))
                throw new ForbiddenException(AppMessages.CommerceHasNoUser);

            var commercePrincipal = await _savingsAccountService.GetActiveByClientAsync(commerce.UserId, cancellationToken);
            var principalAccount = commercePrincipal.FirstOrDefault(a => a.Type == "Principal");
            if (principalAccount == null)
                return OperationResult.Failure(AppMessages.CommerceUserHasNoPrincipal);

            // ---- Tarjeta.
            var card = await _creditCardRepository.GetByCardNumberAsync(request.CardNumber, cancellationToken);
            if (card == null) return OperationResult.Failure(AppMessages.InvalidCardData);
            if (!card.IsActive) return OperationResult.Failure(AppMessages.InvalidCardData);
            if (IsExpired(request.MonthExpirationCard, request.YearExpirationCard) || IsExpired(card.ExpirationDate))
                return OperationResult.Failure(AppMessages.InvalidCardData);

            // El CVC recibido debe coincidir con el hash almacenado. Nunca se compara en texto plano.
            if (!_cryptoService.Verify(request.Cvc ?? string.Empty, card.CvcHash))
                return OperationResult.Failure(AppMessages.InvalidCardData);

            // ---- Credito disponible.
            if (amount > card.AvailableCredit)
            {
                await RegisterRejectedAsync(card, amount, commerce, AppMessages.PaymentExceedsAvailableCredit,
                    cancellationToken);
                return OperationResult.Failure(AppMessages.PaymentExceedsAvailableCredit);
            }

            var now = DateTime.Now;

            await using (var dbTransaction = await _unitOfWork.BeginTransactionAsync(cancellationToken))
            {
                try
                {
                    // Aumenta la deuda de la tarjeta.
                    card.Debt = Money.Round(card.Debt + amount);
                    await _creditCardRepository.UpdateEntityAsync(card);

                    // Registra el consumo APROBADO asociado al comercio.
                    await _creditCardRepository.AddConsumptionAsync(new Consumption
                    {
                        CreditCardId = card.Id,
                        Amount = amount,
                        CommerceName = commerce.Name,
                        CommerceId = commerce.Id,
                        Status = ConsumptionStatus.Aprobado,
                        CreatedAt = now
                    }, cancellationToken);

                    // Acredita a la cuenta principal del comercio.
                    var credit = await _transactionService.RegisterExternalCreditAsync(new ExternalCreditRequest
                    {
                        TargetAccountNumber = principalAccount.AccountNumber,
                        Amount = amount,
                        Operation = TransactionOperation.PagoComercio,
                        OriginLabel = card.LastFourDigits,
                        Actor = OperationActor.Of(request.AuthenticatedUserId,
                            request.IsCommerceRole ? Roles.Comercio : Roles.Administrador)
                    }, cancellationToken);

                    if (!credit.Succeeded)
                        throw new BusinessRuleException(credit.ErrorMessage ?? AppMessages.CommerceUserHasNoPrincipal);

                    await dbTransaction.CommitAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    await dbTransaction.RollbackAsync(cancellationToken);
                    _logger.LogError(ex, "Fallo Hermes Pay para el comercio {CommerceId}. No se aplico ningun movimiento.",
                        commerce.Id);
                    throw;
                }
            }

            _logger.LogInformation("Hermes Pay aprobado. Comercio {CommerceId}. Tarjeta ****{Last}. Monto {Amount}.",
                commerce.Id, card.LastFourDigits, amount);

            // Correos fuera de la transaccion: su fallo no revierte el pago.
            await NotifyAsync(card, commerce, amount, now, cancellationToken);

            return OperationResult.Success();
        }

        // ================================================================== helpers

        private async Task RegisterRejectedAsync(CreditCard card, decimal amount, Commerce commerce, string reason,
            CancellationToken cancellationToken)
        {
            // Registro RECHAZADO: no aumenta deuda ni acredita al comercio.
            await _creditCardRepository.AddConsumptionAsync(new Consumption
            {
                CreditCardId = card.Id,
                Amount = amount,
                CommerceName = commerce.Name,
                CommerceId = commerce.Id,
                Status = ConsumptionStatus.Rechazado,
                RejectionReason = reason,
                CreatedAt = DateTime.Now
            }, cancellationToken);

            _logger.LogInformation("Hermes Pay RECHAZADO. Comercio {CommerceId}. Tarjeta ****{Last}. Motivo: {Reason}.",
                commerce.Id, card.LastFourDigits, reason);
        }

        private async Task NotifyAsync(CreditCard card, Commerce commerce, decimal amount, DateTime moment,
            CancellationToken cancellationToken)
        {
            var owner = await _userReadService.GetByIdAsync(card.ClientId);
            var clientMailOk = await _emailService.SendAsync(new EmailRequest
            {
                To = owner?.Email ?? string.Empty,
                Subject = EmailTemplates.HermesConsumptionSubject(card.LastFourDigits),
                HtmlBody = EmailTemplates.HermesConsumptionBody(owner?.FullName ?? string.Empty, amount,
                    card.LastFourDigits, commerce.Name, moment)
            }, cancellationToken);

            var commerceMailOk = await _emailService.SendAsync(new EmailRequest
            {
                To = commerce.Email,
                Subject = EmailTemplates.HermesPaymentReceivedSubject(card.LastFourDigits),
                HtmlBody = EmailTemplates.HermesPaymentReceivedBody(commerce.Name, amount, card.LastFourDigits, moment)
            }, cancellationToken);

            if (!clientMailOk || !commerceMailOk)
                _logger.LogWarning("Hermes Pay: el pago del comercio {CommerceId} se aplico, pero fallo algun correo.",
                    commerce.Id);
        }

        private static int NormalizePageSize(int pageSize)
        {
            if (pageSize <= 0) return PagedResult<CommerceTransactionDto>.MaxPageSize;
            return pageSize > PagedResult<CommerceTransactionDto>.MaxPageSize
                ? PagedResult<CommerceTransactionDto>.MaxPageSize
                : pageSize;
        }

        /// <summary>Vencida si el ultimo dia del mes de expiracion ya paso. Acepta yyyy o yy.</summary>
        private static bool IsExpired(string month, string year)
        {
            if (!int.TryParse(month, out var m) || m < 1 || m > 12) return true;
            if (!int.TryParse(year, out var y)) return true;
            if (y < 100) y += 2000;

            var lastValidDay = new DateTime(y, m, 1).AddMonths(1).AddDays(-1);
            return DateTime.Now.Date > lastValidDay;
        }

        private static bool IsExpired(string expirationMmYy)
        {
            if (string.IsNullOrWhiteSpace(expirationMmYy) || !expirationMmYy.Contains('/')) return true;
            var parts = expirationMmYy.Split('/');
            return parts.Length == 2 && IsExpired(parts[0], parts[1]);
        }
    }
}
