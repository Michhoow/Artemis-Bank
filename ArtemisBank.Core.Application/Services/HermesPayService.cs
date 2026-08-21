using ArtemisBank.Core.Application.Common.Constants;
using ArtemisBank.Core.Application.Common.Exceptions;
using ArtemisBank.Core.Application.Common.Models;
using ArtemisBank.Core.Application.Dtos.HermesPay;
using ArtemisBank.Core.Application.Interfaces;
using ArtemisBank.Core.Application.Interfaces.Contracts;
using ArtemisBank.Core.Domain.Common.Enums;
using ArtemisBank.Core.Domain.Entities;
using ArtemisBank.Core.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ArtemisBank.Core.Application.Services
{
    public class HermesPayService : IHermesPayService
    {
        private const int CardNumberLength = 16;

        private readonly ICreditCardRepository _cardRepository;
        private readonly ICardConsumptionRepository _consumptionRepository;
        private readonly ICommerceRepository _commerceRepository;
        private readonly ISavingsAccountRepository _accountRepository;
        private readonly IUserReadService _userReadService;
        private readonly ICryptoService _crypto;
        private readonly ITransactionService _transactionService;
        private readonly IEmailService _emailService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<HermesPayService> _logger;

        public HermesPayService(
            ICreditCardRepository cardRepository,
            ICardConsumptionRepository consumptionRepository,
            ICommerceRepository commerceRepository,
            ISavingsAccountRepository accountRepository,
            IUserReadService userReadService,
            ICryptoService crypto,
            ITransactionService transactionService,
            IEmailService emailService,
            IUnitOfWork unitOfWork,
            ILogger<HermesPayService> logger)
        {
            _cardRepository = cardRepository;
            _consumptionRepository = consumptionRepository;
            _commerceRepository = commerceRepository;
            _accountRepository = accountRepository;
            _userReadService = userReadService;
            _crypto = crypto;
            _transactionService = transactionService;
            _emailService = emailService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<HermesPayResultDto> ProcessPaymentAsync(HermesPayRequestDto request, int commerceId,
            string? performedByUserId, CancellationToken cancellationToken = default)
        {
            var cardNumber = (request.CardNumber ?? string.Empty).Trim();

            if (cardNumber.Length != CardNumberLength || !cardNumber.All(char.IsDigit))
                throw new BusinessRuleException(AppMessages.HermesInvalidCardNumber);

            var amount = Money.Round(request.TransactionAmount);
            if (amount <= 0m)
                throw new BusinessRuleException(AppMessages.HermesAmountGreaterThanZero);

            if (!int.TryParse((request.MonthExpirationCard ?? string.Empty).Trim(), out var month)
                || month is < 1 or > 12)
                throw new BusinessRuleException(AppMessages.HermesInvalidExpirationMonth);

            var rawYear = (request.YearExpirationCard ?? string.Empty).Trim();
            if (!int.TryParse(rawYear, out var year) || year <= 0)
                throw new BusinessRuleException(AppMessages.HermesInvalidExpirationYear);
            if (rawYear.Length <= 2) year += 2000;

            var commerce = await _commerceRepository.GetByIdAsync(commerceId)
                           ?? throw new NotFoundException(AppMessages.CommerceNotFound);

            if (!commerce.IsActive)
                throw new BusinessRuleException(AppMessages.CommerceInactive);

            var commerceAccount = await _accountRepository.GetByAccountNumberAsync(commerce.AccountNumber);
            if (commerceAccount == null || !commerceAccount.IsActive)
                throw new BusinessRuleException(AppMessages.CommerceWithoutPrincipalAccount);

            var card = await _cardRepository.GetByCardNumberAsync(cardNumber, cancellationToken);

            if (card == null)
                throw new BusinessRuleException(AppMessages.HermesInvalidCvc);

            if (!_crypto.Verify(request.Cvc ?? string.Empty, card.CvcSalt, card.CvcHash))
            {
                _logger.LogWarning(
                    "Hermes Pay: intento de cobro con código de seguridad incorrecto sobre la tarjeta ****{Last4}.",
                    card.LastFourDigits);
                throw new BusinessRuleException(AppMessages.HermesInvalidCvc);
            }

            if (card.ExpirationMonth != month || card.ExpirationYear != year)
                throw new BusinessRuleException(AppMessages.HermesExpirationMismatch);

            if (!card.IsActive)
                throw new BusinessRuleException(AppMessages.CardNotActive);

            if (card.IsExpired())
                throw new BusinessRuleException(AppMessages.CardExpired);

            var reference = Guid.NewGuid().ToString("N");
            var now = DateTime.Now;

            var description = commerce.Name;

            if (Money.Round(card.Debt + amount) > card.CreditLimit)
            {
                await _consumptionRepository.AddAsync(new CardConsumption
                {
                    CreditCardId = card.Id,
                    Amount = amount,
                    Type = ConsumptionType.Consumo,
                    Status = ConsumptionStatus.Rechazado,
                    Description = description,
                    CommerceName = commerce.Name,
                    RejectionReason = AppMessages.HermesInsufficientCredit,
                    OperationReference = reference,
                    CreatedAt = now,
                    CreatedByUserId = performedByUserId
                });

                _logger.LogInformation(
                    "Hermes Pay: cobro RECHAZADO por crédito insuficiente. Tarjeta ****{Last4}. Comercio {CommerceId}. Referencia {Reference}.",
                    card.LastFourDigits, commerceId, reference);

                throw new BusinessRuleException(AppMessages.HermesInsufficientCredit);
            }

            await using (var dbTransaction = await _unitOfWork.BeginTransactionAsync(cancellationToken))
            {
                try
                {
                    card.Debt = Money.Round(card.Debt + amount);
                    await _cardRepository.UpdateEntityAsync(card);

                    await _consumptionRepository.AddAsync(new CardConsumption
                    {
                        CreditCardId = card.Id,
                        Amount = amount,
                        Type = ConsumptionType.Consumo,
                        Status = ConsumptionStatus.Aprobado,
                        Description = description,
                        CommerceName = commerce.Name,
                        OperationReference = reference,
                        CreatedAt = now,
                        CreatedByUserId = performedByUserId
                    });

                    var credit = await _transactionService.RegisterExternalCreditAsync(new ExternalCreditRequest
                    {
                        TargetAccountNumber = commerce.AccountNumber,
                        Amount = amount,
                        Operation = TransactionOperation.PagoComercio,

                        OriginLabel = card.LastFourDigits,
                        Actor = OperationActor.Of(performedByUserId, Roles.Comercio)
                    }, cancellationToken);

                    if (!credit.Succeeded)
                        throw new BusinessRuleException(
                            credit.ErrorMessage ?? AppMessages.CommerceWithoutPrincipalAccount);

                    await dbTransaction.CommitAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    await dbTransaction.RollbackAsync(cancellationToken);
                    _logger.LogError(ex,
                        "Hermes Pay: fallo el cobro {Reference}. No se aplicó ningún movimiento.", reference);
                    throw;
                }
            }

            _logger.LogInformation(
                "Hermes Pay: cobro APROBADO. Referencia {Reference}. Monto {Amount}. Tarjeta ****{Last4}. Comercio {CommerceId}.",
                reference, amount, card.LastFourDigits, commerceId);

            await NotifyAsync(card, commerce, commerceAccount, amount, now, cancellationToken);

            return new HermesPayResultDto
            {
                Approved = true,
                Status = DisplayText.ConsumptionApproved,
                Message = AppMessages.HermesApproved,
                Amount = amount,
                CardLastFourDigits = card.LastFourDigits,
                CommerceName = commerce.Name,
                CommerceAccountNumber = commerce.AccountNumber,
                Reference = reference,
                ProcessedAt = now
            };
        }

        public async Task<CommerceTransactionsDto> GetTransactionsAsync(int commerceId, int page,
            int pageSize, CancellationToken cancellationToken = default)
        {
            if (page <= 0) page = 1;
            if (pageSize is <= 0 or > 20) pageSize = 20;

            var commerce = await _commerceRepository.GetByIdAsync(commerceId)
                           ?? throw new NotFoundException(AppMessages.CommerceNotFound);

            var query = _consumptionRepository.GetAllQuery()
                .Where(c => c.CommerceName == commerce.Name && c.Type == ConsumptionType.Consumo)
                .OrderByDescending(c => c.CreatedAt)
                .ThenByDescending(c => c.Id);

            var total = await query.CountAsync(cancellationToken);

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new
                {
                    c.Id,
                    c.CreatedAt,
                    c.Amount,
                    c.Status,
                    Last4 = c.CreditCard!.CardNumber
                })
                .ToListAsync(cancellationToken);

            return new CommerceTransactionsDto
            {
                Page = page,
                PageSize = pageSize,
                TotalRecords = total,
                TotalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)pageSize),
                CommerceId = commerce.Id,
                CommerceName = commerce.Name,
                Data = items.Select(c => new CommerceTransactionDto
                {
                    Id = c.Id.ToString(),
                    TransactionDate = c.CreatedAt,
                    Amount = c.Amount,

                    CardLastFourDigits = string.IsNullOrEmpty(c.Last4) || c.Last4.Length < 4
                        ? c.Last4
                        : c.Last4[^4..],
                    Status = c.Status == ConsumptionStatus.Aprobado
                        ? DisplayText.ConsumptionApproved
                        : DisplayText.ConsumptionRejected
                }).ToList()
            };
        }

        private async Task NotifyAsync(CreditCard card, Commerce commerce, SavingsAccount commerceAccount,
            decimal amount, DateTime moment, CancellationToken cancellationToken)
        {
            var cardOwner = await _userReadService.GetByIdAsync(card.ClientId);

            await TrySendAsync(cardOwner?.Email,
                EmailTemplates.CommercePaymentClientSubject(commerce.Name),
                EmailTemplates.CommercePaymentClientBody(cardOwner?.FullName ?? string.Empty,
                    amount, commerce.Name, card.LastFourDigits, moment),
                cancellationToken);

            await TrySendAsync(commerce.Email,
                EmailTemplates.CommercePaymentReceivedSubject(commerceAccount.LastFourDigits),
                EmailTemplates.CommercePaymentReceivedBody(commerce.Name, amount,
                    commerceAccount.LastFourDigits, card.LastFourDigits, moment),
                cancellationToken);
        }

        private async Task<bool> TrySendAsync(string? to, string subject, string body,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(to)) return false;

            try
            {
                return await _emailService.SendAsync(
                    new EmailRequest { To = to, Subject = subject, HtmlBody = body }, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No fue posible enviar una notificación de Hermes Pay.");
                return false;
            }
        }
    }
}
