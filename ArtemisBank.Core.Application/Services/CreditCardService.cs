using ArtemisBank.Core.Application.Common.Constants;
using ArtemisBank.Core.Application.Common.Exceptions;
using ArtemisBank.Core.Application.Common.Models;
using ArtemisBank.Core.Application.Dtos.Common;
using ArtemisBank.Core.Application.Dtos.CreditCards;
using ArtemisBank.Core.Application.Interfaces;
using ArtemisBank.Core.Application.Interfaces.Contracts;
using ArtemisBank.Core.Domain.Common.Enums;
using ArtemisBank.Core.Domain.Entities;
using ArtemisBank.Core.Domain.Interfaces;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ArtemisBank.Core.Application.Services
{
    public class CreditCardService : GenericService<CreditCard, CreditCardDto>, ICreditCardService
    {
        public const decimal CashAdvanceInterestRate = 0.0625m;

        private const int CardNumberLength = 16;
        private const int CvcLength = 3;
        private const int ExpirationYears = 3;
        private const int MaxGenerationAttempts = 25;

        private readonly ICreditCardRepository _cardRepository;
        private readonly ICardConsumptionRepository _consumptionRepository;
        private readonly ISavingsAccountRepository _accountRepository;
        private readonly IUserReadService _userReadService;
        private readonly ICryptoService _crypto;
        private readonly ITransactionService _transactionService;
        private readonly IEmailService _emailService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CreditCardService> _logger;

        public CreditCardService(
            ICreditCardRepository cardRepository,
            ICardConsumptionRepository consumptionRepository,
            ISavingsAccountRepository accountRepository,
            IUserReadService userReadService,
            ICryptoService crypto,
            ITransactionService transactionService,
            IEmailService emailService,
            IUnitOfWork unitOfWork,
            IGenericRepository<CreditCard> genericRepository,
            IMapper mapper,
            ILogger<CreditCardService> logger)
            : base(genericRepository, mapper)
        {
            _cardRepository = cardRepository;
            _consumptionRepository = consumptionRepository;
            _accountRepository = accountRepository;
            _userReadService = userReadService;
            _crypto = crypto;
            _transactionService = transactionService;
            _emailService = emailService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public override Task<CreditCardDto?> AddAsync(CreditCardDto dto)
            => throw new BusinessRuleException(
                "Las tarjetas se emiten mediante CreateAsync, que genera el número y el hash del CVC.");

        public override Task<CreditCardDto?> UpdateAsync(CreditCardDto dto, int id)
            => throw new BusinessRuleException(
                "El único cambio permitido sobre una tarjeta es el límite, mediante UpdateLimitAsync.");

        public override Task<bool> DeleteAsync(int id)
            => throw new BusinessRuleException(
                "Las tarjetas no se eliminan: se cancelan mediante CancelAsync.");

        public async Task<PagedResult<CreditCardDto>> GetPagedAsync(CreditCardFilterDto filter,
            CancellationToken cancellationToken = default)
        {
            var page = filter.Page <= 0 ? 1 : filter.Page;
            var pageSize = filter.PageSize is <= 0 or > PagedResult<CreditCardDto>.MaxPageSize
                ? PagedResult<CreditCardDto>.MaxPageSize
                : filter.PageSize;

            var status = (filter.Status ?? "activa").Trim().ToLowerInvariant();

            if (!string.IsNullOrWhiteSpace(filter.Identification))
            {
                var client = await _userReadService.GetByIdentificationAsync(filter.Identification.Trim());
                if (client == null) return PagedResult<CreditCardDto>.Empty(page, pageSize);

                return await BuildPageAsync(
                    _cardRepository.Query().Where(c => c.ClientId == client.Id), status, page, pageSize,
                    cancellationToken);
            }

            return await BuildPageAsync(_cardRepository.Query(), status, page, pageSize, cancellationToken);
        }

        private async Task<PagedResult<CreditCardDto>> BuildPageAsync(IQueryable<CreditCard> baseQuery,
            string status, int page, int pageSize, CancellationToken cancellationToken)
        {
            var query = baseQuery.AsNoTracking();

            query = status switch
            {
                "cancelada" => query.Where(c => c.Status == CardStatus.Cancelada),
                "todas" => query,
                _ => query.Where(c => c.Status == CardStatus.Activa)
            };

            query = query.OrderByDescending(c => c.CreatedAt).ThenByDescending(c => c.Id);

            var total = await query.CountAsync(cancellationToken);

            var cards = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var dtos = await MapWithClientAsync(cards);
            return PagedResult<CreditCardDto>.Create(dtos, page, pageSize, total);
        }

        public async Task<CreditCardDetailDto?> GetDetailAsync(int creditCardId,
            CancellationToken cancellationToken = default)
        {
            var card = await _cardRepository.GetWithConsumptionsAsync(creditCardId, cancellationToken);
            if (card == null) return null;

            var owner = await _userReadService.GetByIdAsync(card.ClientId);
            var basic = MapBasic(card, owner);

            return new CreditCardDetailDto
            {
                Id = basic.Id,
                ClientId = basic.ClientId,
                ClientFullName = basic.ClientFullName,
                ClientIdentification = basic.ClientIdentification,
                MaskedNumber = basic.MaskedNumber,
                LastFourDigits = basic.LastFourDigits,
                CreditLimit = basic.CreditLimit,
                Debt = basic.Debt,
                AvailableCredit = basic.AvailableCredit,
                ExpirationDate = basic.ExpirationDate,
                Status = basic.Status,
                IsActive = basic.IsActive,
                CreatedAt = basic.CreatedAt,

                Consumptions = (card.Consumptions ?? new List<CardConsumption>())
                    .OrderByDescending(k => k.CreatedAt)
                    .ThenByDescending(k => k.Id)
                    .Select(MapConsumption)
                    .ToList()
            };
        }

        public async Task<List<CreditCardDto>> GetByClientAsync(string clientId, bool onlyActive = true,
            CancellationToken cancellationToken = default)
        {
            var cards = onlyActive
                ? await _cardRepository.GetActiveByClientAsync(clientId, cancellationToken)
                : await _cardRepository.GetAllByClientAsync(clientId, cancellationToken);

            return await MapWithClientAsync(cards);
        }

        public async Task<CreatedCreditCardDto> CreateAsync(CreateCreditCardDto request, string? adminUserId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.ClientId))
                throw new BusinessRuleException(AppMessages.CardClientRequired);

            if (request.CreditLimit <= 0m)
                throw new BusinessRuleException(AppMessages.CardLimitGreaterThanZero);

            var client = await _userReadService.GetByIdAsync(request.ClientId)
                         ?? throw new NotFoundException(AppMessages.LoanClientNotFound);

            if (!client.IsActive)
                throw new BusinessRuleException(AppMessages.CardOnlyActiveClients);

            var cardNumber = await GenerateUniqueCardNumberAsync(cancellationToken);

            var cvc = _crypto.RandomDigits(CvcLength);
            var salt = _crypto.GenerateSalt();

            var now = DateTime.Now;
            var expiration = now.AddYears(ExpirationYears);

            var card = new CreditCard
            {
                CardNumber = cardNumber,
                ClientId = request.ClientId,
                CreditLimit = Money.Round(request.CreditLimit),
                Debt = 0m,
                ExpirationMonth = expiration.Month,
                ExpirationYear = expiration.Year,
                CvcHash = _crypto.Hash(cvc, salt),
                CvcSalt = salt,
                Status = CardStatus.Activa,
                CreatedAt = now,
                CreatedByUserId = adminUserId
            };

            await _cardRepository.AddAsync(card);

            _logger.LogInformation(
                "Tarjeta ****{Last4} emitida al cliente {ClientId}. Limite {Limit}. Administrador {AdminId}.",
                card.LastFourDigits, request.ClientId, card.CreditLimit, adminUserId);

            await TrySendAsync(client.Email,
                EmailTemplates.CardAssignedSubject(card.LastFourDigits),
                EmailTemplates.CardAssignedBody(client.FullName, card.LastFourDigits,
                    card.CreditLimit, card.ExpirationDisplay, now),
                cancellationToken);

            return new CreatedCreditCardDto
            {
                Card = MapBasic(card, client),
                Cvc = cvc
            };
        }

        public async Task<CreditCardDto> UpdateLimitAsync(int creditCardId, decimal newLimit, string? adminUserId,
            CancellationToken cancellationToken = default)
        {
            if (newLimit <= 0m)
                throw new BusinessRuleException(AppMessages.CardLimitGreaterThanZero);

            var card = await _cardRepository.GetByIdAsync(creditCardId)
                       ?? throw new NotFoundException(AppMessages.CardNotFound);

            if (!card.IsActive)
                throw new BusinessRuleException(AppMessages.CardNotActive);

            var limit = Money.Round(newLimit);

            if (limit < card.Debt)
                throw new BusinessRuleException(AppMessages.CardLimitBelowDebt);

            card.CreditLimit = limit;
            await _cardRepository.UpdateEntityAsync(card);

            _logger.LogInformation(
                "Limite de la tarjeta ****{Last4} modificado a {Limit}. Administrador {AdminId}.",
                card.LastFourDigits, limit, adminUserId);

            var client = await _userReadService.GetByIdAsync(card.ClientId);
            await TrySendAsync(client?.Email,
                EmailTemplates.CardLimitChangedSubject(card.LastFourDigits),
                EmailTemplates.CardLimitChangedBody(client?.FullName ?? string.Empty,
                    card.LastFourDigits, limit, DateTime.Now),
                cancellationToken);

            return MapBasic(card, client);
        }

        public async Task CancelAsync(int creditCardId, string? adminUserId,
            CancellationToken cancellationToken = default)
        {
            var card = await _cardRepository.GetByIdAsync(creditCardId)
                       ?? throw new NotFoundException(AppMessages.CardNotFound);

            if (card.Status == CardStatus.Cancelada)
                throw new BusinessRuleException(AppMessages.CardAlreadyCancelled);

            if (card.Debt > 0m)
                throw new BusinessRuleException(AppMessages.CardCannotCancelWithDebt);

            card.Status = CardStatus.Cancelada;
            card.CancelledAt = DateTime.Now;
            card.CancelledByUserId = adminUserId;

            await _cardRepository.UpdateEntityAsync(card);

            _logger.LogInformation("Tarjeta ****{Last4} cancelada. Administrador {AdminId}.",
                card.LastFourDigits, adminUserId);

            var client = await _userReadService.GetByIdAsync(card.ClientId);
            await TrySendAsync(client?.Email,
                EmailTemplates.CardCancelledSubject(card.LastFourDigits),
                EmailTemplates.CardCancelledBody(client?.FullName ?? string.Empty,
                    card.LastFourDigits, DateTime.Now),
                cancellationToken);
        }

        public async Task<CashAdvanceResultDto> CashAdvanceAsync(CashAdvanceRequestDto request, string clientId,
            CancellationToken cancellationToken = default)
        {
            var amount = Money.Round(request.Amount);
            if (amount <= 0m)
                throw new BusinessRuleException(AppMessages.AdvanceAmountGreaterThanZero);

            if (string.IsNullOrWhiteSpace(request.TargetAccountNumber))
                throw new BusinessRuleException(AppMessages.AdvanceTargetAccountRequired);

            var card = await _cardRepository.GetByIdAsync(request.CreditCardId)
                       ?? throw new NotFoundException(AppMessages.CardNotFound);

            if (!string.Equals(card.ClientId, clientId, StringComparison.Ordinal))
                throw new ForbiddenException(AppMessages.CardNotFound);

            if (!card.IsActive)
                throw new BusinessRuleException(AppMessages.CardNotActive);

            if (card.IsExpired())
                throw new BusinessRuleException(AppMessages.CardExpired);

            var target = await _accountRepository.GetByAccountNumberAsync(request.TargetAccountNumber);
            if (target == null || !target.IsActive ||
                !string.Equals(target.ClientId, clientId, StringComparison.Ordinal))
                throw new BusinessRuleException(AppMessages.AdvanceTargetAccountInvalid);

            var interest = Money.Round(amount * CashAdvanceInterestRate);
            var totalCharged = Money.Round(amount + interest);
            var reference = Guid.NewGuid().ToString("N");
            var now = DateTime.Now;

            if (totalCharged > card.AvailableCredit)
            {
                await _consumptionRepository.AddAsync(new CardConsumption
                {
                    CreditCardId = card.Id,
                    Amount = totalCharged,
                    Type = ConsumptionType.AvanceEfectivo,
                    Status = ConsumptionStatus.Rechazado,
                    Description = DisplayText.ConsumptionType(ConsumptionType.AvanceEfectivo),
                    RejectionReason = AppMessages.AdvanceInsufficientCredit,
                    OperationReference = reference,
                    CreatedAt = now,
                    CreatedByUserId = clientId
                });

                throw new BusinessRuleException(AppMessages.AdvanceInsufficientCredit);
            }

            await using (var dbTransaction = await _unitOfWork.BeginTransactionAsync(cancellationToken))
            {
                try
                {
                    card.Debt = Money.Round(card.Debt + totalCharged);
                    await _cardRepository.UpdateEntityAsync(card);

                    await _consumptionRepository.AddRangeAsync(new List<CardConsumption>
                    {
                        new CardConsumption
                        {
                            CreditCardId = card.Id,
                            Amount = amount,
                            Type = ConsumptionType.AvanceEfectivo,
                            Status = ConsumptionStatus.Aprobado,
                            Description = DisplayText.ConsumptionType(ConsumptionType.AvanceEfectivo),
                            OperationReference = reference,
                            CreatedAt = now,
                            CreatedByUserId = clientId
                        },
                        new CardConsumption
                        {
                            CreditCardId = card.Id,
                            Amount = interest,
                            Type = ConsumptionType.InteresAvance,
                            Status = ConsumptionStatus.Aprobado,
                            Description = DisplayText.ConsumptionType(ConsumptionType.InteresAvance),
                            OperationReference = reference,
                            CreatedAt = now,
                            CreatedByUserId = clientId
                        }
                    });

                    var credit = await _transactionService.RegisterExternalCreditAsync(new ExternalCreditRequest
                    {
                        TargetAccountNumber = target.AccountNumber,
                        Amount = amount,
                        Operation = TransactionOperation.AvanceEfectivo,
                        OriginLabel = card.LastFourDigits,
                        Actor = OperationActor.Of(clientId, Roles.Cliente)
                    }, cancellationToken);

                    if (!credit.Succeeded)
                        throw new BusinessRuleException(
                            credit.ErrorMessage ?? AppMessages.AdvanceTargetAccountInvalid);

                    await dbTransaction.CommitAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    await dbTransaction.RollbackAsync(cancellationToken);
                    _logger.LogError(ex,
                        "Fallo el avance de efectivo {Reference}. No se aplico ningun movimiento.", reference);
                    throw;
                }
            }

            _logger.LogInformation(
                "Avance de efectivo aprobado. Referencia {Reference}. Tarjeta ****{Last4}. Monto {Amount}. Interes {Interest}.",
                reference, card.LastFourDigits, amount, interest);

            var client = await _userReadService.GetByIdAsync(clientId);
            await TrySendAsync(client?.Email,
                EmailTemplates.CashAdvanceSubject(card.LastFourDigits),
                EmailTemplates.CashAdvanceBody(client?.FullName ?? string.Empty, amount, interest,
                    totalCharged, card.LastFourDigits, target.LastFourDigits, now),
                cancellationToken);

            return new CashAdvanceResultDto
            {
                Amount = amount,
                InterestAmount = interest,
                TotalCharged = totalCharged,
                CardLastFourDigits = card.LastFourDigits,
                TargetAccountNumber = target.AccountNumber,
                Reference = reference
            };
        }

        private async Task<string> GenerateUniqueCardNumberAsync(CancellationToken cancellationToken)
        {
            for (var attempt = 0; attempt < MaxGenerationAttempts; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var candidate = _crypto.RandomDigits(CardNumberLength);
                if (!await _cardRepository.CardNumberExistsAsync(candidate, cancellationToken))
                    return candidate;
            }

            throw new ConflictException(AppMessages.CardCouldNotGenerateNumber);
        }

        private async Task<List<CreditCardDto>> MapWithClientAsync(List<CreditCard> cards)
        {
            if (cards.Count == 0) return new List<CreditCardDto>();

            var owners = await _userReadService.GetByIdsAsync(cards.Select(c => c.ClientId).Distinct());
            var byId = owners.ToDictionary(o => o.Id, StringComparer.Ordinal);

            return cards.Select(card =>
            {
                byId.TryGetValue(card.ClientId, out var owner);
                return MapBasic(card, owner);
            }).ToList();
        }

        private static CreditCardDto MapBasic(CreditCard card, UserInfoDto? owner) => new CreditCardDto
        {
            Id = card.Id,
            ClientId = card.ClientId,
            ClientFullName = owner?.FullName ?? string.Empty,
            ClientIdentification = owner?.Identification ?? string.Empty,
            MaskedNumber = Money.MaskCard(card.CardNumber),
            LastFourDigits = card.LastFourDigits,
            CreditLimit = card.CreditLimit,
            Debt = card.Debt,
            AvailableCredit = card.AvailableCredit,
            ExpirationDate = card.ExpirationDisplay,
            Status = DisplayText.CardStatus(card.Status),
            IsActive = card.IsActive,
            CreatedAt = card.CreatedAt
        };

        private static CardConsumptionDto MapConsumption(CardConsumption consumption) => new CardConsumptionDto
        {
            Id = consumption.Id,
            CreatedAt = consumption.CreatedAt,
            Amount = consumption.Amount,
            Type = DisplayText.ConsumptionType(consumption.Type),
            Status = DisplayText.ConsumptionStatus(consumption.Status),
            Description = consumption.Description,
            CommerceName = consumption.CommerceName,
            RejectionReason = consumption.RejectionReason
        };

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
                _logger.LogWarning(ex, "No fue posible enviar una notificacion del modulo de tarjetas.");
                return false;
            }
        }
    }
}
