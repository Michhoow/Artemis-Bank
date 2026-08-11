using ArtemisBank.Core.Application.Common.Constants;
using ArtemisBank.Core.Application.Common.Exceptions;
using ArtemisBank.Core.Application.Common.Models;
using ArtemisBank.Core.Application.Dtos.Common;
using ArtemisBank.Core.Application.Dtos.SavingsAccounts;
using ArtemisBank.Core.Application.Dtos.Transactions;
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
    /// <summary>
    /// Reglas de negocio de cuentas de ahorro.
    /// Solo crea cuentas SECUNDARIAS: la principal la crea el modulo de usuarios.
    /// </summary>
    public class SavingsAccountService
        : GenericService<SavingsAccount, SavingsAccountDto>, ISavingsAccountService
    {
        private readonly ISavingsAccountRepository _accountRepository;
        private readonly ITransactionRepository _transactionRepository;
        private readonly IAccountNumberGenerator _numberGenerator;
        private readonly IUserReadService _userReadService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<SavingsAccountService> _logger;

        public SavingsAccountService(
            ISavingsAccountRepository accountRepository,
            ITransactionRepository transactionRepository,
            IAccountNumberGenerator numberGenerator,
            IUserReadService userReadService,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<SavingsAccountService> logger)
            : base(accountRepository, mapper)
        {
            _accountRepository = accountRepository;
            _transactionRepository = transactionRepository;
            _numberGenerator = numberGenerator;
            _userReadService = userReadService;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        // ------------------------------------------------------------------ consultas

        public async Task<PagedResult<SavingsAccountDto>> GetPagedAsync(SavingsAccountFilterDto filter,
            CancellationToken cancellationToken = default)
        {
            var search = await SearchAsync(filter, cancellationToken);
            return search.Result;
        }

        public async Task<(PagedResult<SavingsAccountDto> Result, string? InfoMessage)> SearchAsync(
            SavingsAccountFilterDto filter, CancellationToken cancellationToken = default)
        {
            var page = filter.Page <= 0 ? 1 : filter.Page;
            var pageSize = filter.PageSize <= 0 ? PagedResult<SavingsAccountDto>.MaxPageSize : filter.PageSize;
            if (pageSize > PagedResult<SavingsAccountDto>.MaxPageSize)
                pageSize = PagedResult<SavingsAccountDto>.MaxPageSize;

            var status = NormalizeStatus(filter.Status);
            var type = NormalizeType(filter.Type);

            var query = _accountRepository.GetAllQuery();
            string? infoMessage = null;
            var searchingByIdentification = !string.IsNullOrWhiteSpace(filter.Identification);

            if (searchingByIdentification)
            {
                var client = await _userReadService.GetByIdentificationAsync(filter.Identification!.Trim());
                if (client == null)
                    return (PagedResult<SavingsAccountDto>.Empty(page, pageSize), AppMessages.ClientNotFoundByIdentification);

                query = query.Where(a => a.ClientId == client.Id);
            }

            if (status == "activa") query = query.Where(a => a.Status == AccountStatus.Activa);
            else if (status == "cancelada") query = query.Where(a => a.Status == AccountStatus.Cancelada);

            if (type == "principal") query = query.Where(a => a.Type == AccountType.Principal);
            else if (type == "secundaria") query = query.Where(a => a.Type == AccountType.Secundaria);

            // Al buscar por cedula sin estado explicito: primero activas, luego canceladas.
            // Dentro de cada grupo, de la mas reciente a la mas antigua.
            query = searchingByIdentification && string.IsNullOrWhiteSpace(filter.Status)
                ? query.OrderBy(a => a.Status).ThenByDescending(a => a.CreatedAt).ThenByDescending(a => a.Id)
                : query.OrderByDescending(a => a.CreatedAt).ThenByDescending(a => a.Id);

            var total = await query.CountAsync(cancellationToken);

            if (total == 0 && searchingByIdentification)
                infoMessage = AppMessages.ClientWithoutAccounts;

            var entities = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var dtos = await ToDtosAsync(entities);
            return (PagedResult<SavingsAccountDto>.Create(dtos, page, pageSize, total), infoMessage);
        }

        public async Task<SavingsAccountDto?> GetByAccountNumberAsync(string accountNumber,
            CancellationToken cancellationToken = default)
        {
            var entity = await _accountRepository.GetByAccountNumberAsync(accountNumber);
            if (entity == null) return null;
            return (await ToDtosAsync(new List<SavingsAccount> { entity })).FirstOrDefault();
        }

        /// <summary>Sobrescribe el generico para completar nombre y cedula del titular.</summary>
        public override async Task<SavingsAccountDto?> GetByIdAsync(int id)
        {
            var entity = await _accountRepository.GetByIdAsync(id);
            if (entity == null) return null;
            return (await ToDtosAsync(new List<SavingsAccount> { entity })).FirstOrDefault();
        }

        /// <summary>
        /// Una cuenta de ahorro NUNCA se elimina fisicamente: se cancela.
        /// Se bloquea el borrado heredado del servicio generico para que nadie lo use por error.
        /// </summary>
        public override Task<bool> DeleteAsync(int id)
            => throw new BusinessRuleException(
                "Las cuentas de ahorro no se eliminan. Utilice la cancelación de cuenta secundaria.");

        /// <summary>
        /// El alta generica queda bloqueada a proposito: crear una cuenta exige validar cliente
        /// activo, cuenta principal activa y generar un numero unico de 9 digitos.
        /// Ese camino es CreateSecondaryAsync.
        /// </summary>
        public override Task<SavingsAccountDto?> AddAsync(SavingsAccountDto dto)
            => throw new BusinessRuleException(
                "Use CreateSecondaryAsync: el alta de cuentas exige validaciones de negocio.");

        /// <summary>
        /// La actualizacion generica queda bloqueada: el balance solo puede moverse a traves de
        /// ITransactionService, que garantiza el registro contable y la atomicidad.
        /// </summary>
        public override Task<SavingsAccountDto?> UpdateAsync(SavingsAccountDto dto, int id)
            => throw new BusinessRuleException(
                "El balance de una cuenta solo se modifica mediante operaciones transaccionales.");

        public Task<SavingsAccount?> GetEntityByNumberAsync(string accountNumber,
            CancellationToken cancellationToken = default)
            => _accountRepository.GetByAccountNumberAsync(accountNumber);

        public Task<int> CountActiveAsync(CancellationToken cancellationToken = default)
            => _accountRepository.CountActiveAsync();

        /// <summary>Principal primero; luego secundarias de mayor a menor balance.</summary>
        public async Task<List<SavingsAccountDto>> GetActiveByClientAsync(string clientId,
            CancellationToken cancellationToken = default)
        {
            var entities = await _accountRepository.GetActiveByClientAsync(clientId);
            var ordered = entities
                .OrderBy(a => a.Type == AccountType.Principal ? 0 : 1)
                .ThenByDescending(a => a.Balance)
                .ToList();
            return await ToDtosAsync(ordered);
        }

        public async Task<SavingsAccountTransactionsDto> GetTransactionsAsync(string accountNumber, int page,
            int pageSize, CancellationToken cancellationToken = default)
        {
            var account = await _accountRepository.GetByAccountNumberAsync(accountNumber);
            if (account == null) throw new NotFoundException(AppMessages.AccountDoesNotExist);

            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = PagedResult<TransactionDto>.MaxPageSize;
            if (pageSize > PagedResult<TransactionDto>.MaxPageSize)
                pageSize = PagedResult<TransactionDto>.MaxPageSize;

            var query = _transactionRepository.GetByAccountQuery(account.Id)
                .OrderByDescending(t => t.CreatedAt).ThenByDescending(t => t.Id);

            var total = await query.CountAsync(cancellationToken);
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

            var owner = await _userReadService.GetByIdAsync(account.ClientId);

            return new SavingsAccountTransactionsDto
            {
                AccountNumber = account.AccountNumber,
                ClientFullName = owner?.FullName ?? string.Empty,
                Balance = account.Balance,
                Type = DisplayText.AccountType(account.Type),
                Status = DisplayText.AccountStatus(account.Status),
                Transactions = PagedResult<TransactionDto>.Create(
                    _mapper.Map<List<TransactionDto>>(items), page, pageSize, total)
            };
        }

        public async Task<List<TransactionDto>> GetLastTransactionsAsync(int accountId, int take,
            CancellationToken cancellationToken = default)
        {
            var items = await _transactionRepository.GetByAccountQuery(accountId)
                .OrderByDescending(t => t.CreatedAt).ThenByDescending(t => t.Id)
                .Take(take <= 0 ? 10 : take)
                .ToListAsync(cancellationToken);
            return _mapper.Map<List<TransactionDto>>(items);
        }

        // ------------------------------------------------------------------ comandos

        public async Task<SavingsAccountDto> CreateSecondaryAsync(string clientId, decimal initialBalance,
            string? createdByUserId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(clientId))
                throw new BusinessRuleException(AppMessages.MustSelectClient);

            var client = await _userReadService.GetByIdAsync(clientId);
            if (client == null)
                throw new NotFoundException(AppMessages.MustSelectClient);

            if (!client.IsActive)
                throw new BusinessRuleException(AppMessages.OnlyActiveClients);

            var principal = await _accountRepository.GetPrincipalByClientAsync(clientId);
            if (principal == null || principal.Status != AccountStatus.Activa)
                throw new BusinessRuleException(AppMessages.ClientNeedsPrincipalAccount);

            if (initialBalance < 0m)
                throw new BusinessRuleException(AppMessages.NegativeInitialBalance);

            initialBalance = Money.Round(initialBalance);
            var accountNumber = await _numberGenerator.GenerateAsync(cancellationToken);
            var now = DateTime.Now;

            await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                var account = new SavingsAccount
                {
                    AccountNumber = accountNumber,
                    ClientId = clientId,
                    Balance = initialBalance,
                    Type = AccountType.Secundaria,
                    Status = AccountStatus.Activa,
                    CreatedAt = now,
                    CreatedByUserId = createdByUserId
                };

                await _accountRepository.AddAsync(account);

                // Todo balance inicial mayor que cero se registra como CREDITO.
                if (initialBalance > 0m)
                {
                    await _transactionRepository.AddAsync(new Transaction
                    {
                        SavingsAccountId = account.Id,
                        Amount = initialBalance,
                        Type = TransactionType.Credito,
                        Status = TransactionStatus.Aprobada,
                        Operation = TransactionOperation.BalanceInicial,
                        Origin = DisplayText.Deposit,
                        Beneficiary = account.AccountNumber,
                        OperationReference = Guid.NewGuid().ToString("N"),
                        PerformedByUserId = createdByUserId,
                        PerformedByRole = Roles.Administrador,
                        CreatedAt = now,
                        CreatedByUserId = createdByUserId
                    });
                }

                await transaction.CommitAsync(cancellationToken);

                _logger.LogInformation(
                    "Cuenta secundaria {AccountNumber} creada para el cliente {ClientId} con balance inicial {Balance}",
                    accountNumber, clientId, initialBalance);

                return (await ToDtosAsync(new List<SavingsAccount> { account })).First();
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        public async Task CancelSecondaryAsync(string accountNumber, string? cancelledByUserId,
            CancellationToken cancellationToken = default)
        {
            var account = await _accountRepository.GetByAccountNumberAsync(accountNumber);
            if (account == null)
                throw new NotFoundException(AppMessages.AccountDoesNotExist);

            if (account.Type == AccountType.Principal)
                throw new BusinessRuleException(AppMessages.PrincipalCannotBeCancelled);

            if (account.Status == AccountStatus.Cancelada)
                throw new BusinessRuleException(AppMessages.AccountAlreadyCancelled);

            var principal = await _accountRepository.GetPrincipalByClientAsync(account.ClientId);
            if (principal == null || principal.Status != AccountStatus.Activa)
                throw new BusinessRuleException(AppMessages.NoPrincipalToReceiveFunds);

            var now = DateTime.Now;
            var balance = Money.Round(account.Balance);
            var reference = Guid.NewGuid().ToString("N");

            await using var dbTransaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                // El traslado del balance ocurre ANTES de cambiar el estado de la cuenta.
                if (balance > 0m)
                {
                    account.Balance = 0m;
                    principal.Balance = Money.Round(principal.Balance + balance);

                    await _accountRepository.UpdateEntityAsync(account);
                    await _accountRepository.UpdateEntityAsync(principal);

                    await _transactionRepository.AddRangeAsync(new List<Transaction>
                    {
                        new Transaction
                        {
                            SavingsAccountId = account.Id,
                            Amount = balance,
                            Type = TransactionType.Debito,
                            Status = TransactionStatus.Aprobada,
                            Operation = TransactionOperation.CancelacionCuenta,
                            Origin = account.AccountNumber,
                            Beneficiary = principal.AccountNumber,
                            OperationReference = reference,
                            PerformedByUserId = cancelledByUserId,
                            PerformedByRole = Roles.Administrador,
                            CreatedAt = now,
                            CreatedByUserId = cancelledByUserId
                        },
                        new Transaction
                        {
                            SavingsAccountId = principal.Id,
                            Amount = balance,
                            Type = TransactionType.Credito,
                            Status = TransactionStatus.Aprobada,
                            Operation = TransactionOperation.CancelacionCuenta,
                            Origin = account.AccountNumber,
                            Beneficiary = principal.AccountNumber,
                            OperationReference = reference,
                            PerformedByUserId = cancelledByUserId,
                            PerformedByRole = Roles.Administrador,
                            CreatedAt = now,
                            CreatedByUserId = cancelledByUserId
                        }
                    });
                }

                account.Status = AccountStatus.Cancelada;
                account.CancelledAt = now;
                account.CancelledByUserId = cancelledByUserId;
                await _accountRepository.UpdateEntityAsync(account);

                await dbTransaction.CommitAsync(cancellationToken);

                _logger.LogInformation(
                    "Cuenta secundaria {AccountNumber} cancelada. Balance trasladado a la principal: {Balance}",
                    account.AccountNumber, balance);
            }
            catch
            {
                await dbTransaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        // ------------------------------------------------------------------ helpers

        private static string NormalizeStatus(string? value)
        {
            var normalized = (value ?? "activa").Trim().ToLowerInvariant();
            if (normalized != "activa" && normalized != "cancelada" && normalized != "todas")
                throw new BusinessRuleException("El parámetro status solo admite los valores activa, cancelada o todas.");
            return normalized;
        }

        private static string NormalizeType(string? value)
        {
            var normalized = (value ?? "todas").Trim().ToLowerInvariant();
            if (normalized != "principal" && normalized != "secundaria" && normalized != "todas")
                throw new BusinessRuleException("El parámetro type solo admite los valores principal, secundaria o todas.");
            return normalized;
        }

        private async Task<List<SavingsAccountDto>> ToDtosAsync(List<SavingsAccount> accounts)
        {
            var dtos = _mapper.Map<List<SavingsAccountDto>>(accounts);
            if (dtos.Count == 0) return dtos;

            var owners = await _userReadService.GetByIdsAsync(accounts.Select(a => a.ClientId).Distinct());
            var byId = owners.ToDictionary(o => o.Id, o => o);

            foreach (var dto in dtos)
            {
                if (byId.TryGetValue(dto.ClientId, out UserInfoDto? owner) && owner != null)
                {
                    dto.ClientFullName = owner.FullName;
                    dto.Identification = owner.Identification;
                }
            }
            return dtos;
        }
    }
}
