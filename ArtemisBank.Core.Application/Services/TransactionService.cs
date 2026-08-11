using ArtemisBank.Core.Application.Common.Constants;
using ArtemisBank.Core.Application.Common.Models;
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
    /// Nucleo de movimiento de dinero del sistema.
    /// Invariantes que garantiza este servicio:
    ///  - DEBITO para salidas, CREDITO para entradas.
    ///  - Registro cruzado en toda transferencia (misma OperationReference).
    ///  - Ejecucion atomica: o se aplican todas las patas o ninguna.
    ///  - Los intentos rechazados se registran pero jamas modifican balances.
    ///  - Un fallo de correo nunca revierte una operacion ya confirmada.
    /// </summary>
    public class TransactionService : ITransactionService
    {
        private readonly ISavingsAccountRepository _accountRepository;
        private readonly ITransactionRepository _transactionRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserReadService _userReadService;
        private readonly ICreditCardReadService _creditCardService;
        private readonly ILoanReadService _loanService;
        private readonly IEmailService _emailService;
        private readonly IMapper _mapper;
        private readonly ILogger<TransactionService> _logger;

        public TransactionService(
            ISavingsAccountRepository accountRepository,
            ITransactionRepository transactionRepository,
            IUnitOfWork unitOfWork,
            IUserReadService userReadService,
            ICreditCardReadService creditCardService,
            ILoanReadService loanService,
            IEmailService emailService,
            IMapper mapper,
            ILogger<TransactionService> logger)
        {
            _accountRepository = accountRepository;
            _transactionRepository = transactionRepository;
            _unitOfWork = unitOfWork;
            _userReadService = userReadService;
            _creditCardService = creditCardService;
            _loanService = loanService;
            _emailService = emailService;
            _mapper = mapper;
            _logger = logger;
        }

        // ================================================================== transferencias

        public async Task<OperationResult> TransferAsync(TransferRequest request,
            CancellationToken cancellationToken = default)
        {
            var amount = Money.Round(request.Amount);
            var isOwnTransfer = request.Operation == TransactionOperation.TransferenciaEntreCuentas;

            if (amount <= 0m)
                return OperationResult.Failure(AppMessages.TransferAmountGreaterThanZero);

            var source = await _accountRepository.GetByAccountNumberAsync(request.SourceAccountNumber);
            if (source == null || !source.IsActive)
                return OperationResult.Failure(InvalidSourceMessage(request.Operation));

            if (!string.IsNullOrEmpty(request.ExpectedSourceOwnerId) &&
                !string.Equals(source.ClientId, request.ExpectedSourceOwnerId, StringComparison.Ordinal))
                return OperationResult.Failure(InvalidSourceMessage(request.Operation));

            var target = await _accountRepository.GetByAccountNumberAsync(request.TargetAccountNumber);
            if (target == null || !target.IsActive)
            {
                await RegisterRejectedAsync(source, amount, TransactionType.Debito, request.Operation,
                    source.AccountNumber, request.TargetAccountNumber, AppMessages.RejectedInvalidProduct,
                    request.Actor, cancellationToken);
                return OperationResult.Failure(InvalidTargetMessage(request.Operation));
            }

            if (source.Id == target.Id)
                return OperationResult.Failure(isOwnTransfer
                    ? AppMessages.SameAccountTransferNotAllowed
                    : AppMessages.SameAccountNotAllowed);

            if (isOwnTransfer &&
                !string.Equals(source.ClientId, target.ClientId, StringComparison.Ordinal))
                return OperationResult.Failure(AppMessages.InvalidAccountNumber);

            if (source.Balance < amount)
            {
                await RegisterRejectedAsync(source, amount, TransactionType.Debito, request.Operation,
                    source.AccountNumber, target.AccountNumber, AppMessages.RejectedInsufficientFunds,
                    request.Actor, cancellationToken);
                return OperationResult.Failure(InsufficientFundsMessage(request.Operation));
            }

            var now = DateTime.Now;
            var reference = Guid.NewGuid().ToString("N");

            await using (var dbTransaction = await _unitOfWork.BeginTransactionAsync(cancellationToken))
            {
                try
                {
                    source.Balance = Money.Round(source.Balance - amount);
                    target.Balance = Money.Round(target.Balance + amount);

                    await _accountRepository.UpdateEntityAsync(source);
                    await _accountRepository.UpdateEntityAsync(target);

                    await _transactionRepository.AddRangeAsync(new List<Transaction>
                    {
                        BuildTransaction(source.Id, amount, TransactionType.Debito, request.Operation,
                            source.AccountNumber, target.AccountNumber, reference, request.Actor, now),
                        BuildTransaction(target.Id, amount, TransactionType.Credito, request.Operation,
                            source.AccountNumber, target.AccountNumber, reference, request.Actor, now)
                    });

                    await dbTransaction.CommitAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    await dbTransaction.RollbackAsync(cancellationToken);
                    _logger.LogError(ex, "Fallo la transferencia {Reference}. No se aplico ningun movimiento.", reference);
                    throw;
                }
            }

            _logger.LogInformation(
                "{Operation} aprobada. Referencia {Reference}. Monto {Amount}. Origen ****{SourceLast} Destino ****{TargetLast}",
                request.Operation, reference, amount, source.LastFourDigits, target.LastFourDigits);

            var mailsOk = true;
            if (request.NotifyByEmail)
                mailsOk = await NotifyTransferAsync(source, target, amount, now, isOwnTransfer, cancellationToken);

            return OperationResult.Success(reference, mailsOk
                ? null
                : (isOwnTransfer ? AppMessages.TransferOkMailFailed : AppMessages.TransactionOkMailFailed));
        }

        // ================================================================== deposito

        public async Task<OperationResult> DepositAsync(DepositRequest request,
            CancellationToken cancellationToken = default)
        {
            var amount = Money.Round(request.Amount);
            if (amount <= 0m)
                return OperationResult.Failure(AppMessages.DepositAmountGreaterThanZero);

            var target = await _accountRepository.GetByAccountNumberAsync(request.TargetAccountNumber);
            if (target == null || !target.IsActive)
                return OperationResult.Failure(AppMessages.InvalidAccountNumber);

            var now = DateTime.Now;
            var reference = Guid.NewGuid().ToString("N");

            await using (var dbTransaction = await _unitOfWork.BeginTransactionAsync(cancellationToken))
            {
                try
                {
                    target.Balance = Money.Round(target.Balance + amount);
                    await _accountRepository.UpdateEntityAsync(target);

                    await _transactionRepository.AddAsync(BuildTransaction(target.Id, amount, TransactionType.Credito,
                        TransactionOperation.Deposito, DisplayText.Deposit, target.AccountNumber,
                        reference, request.Actor, now));

                    await dbTransaction.CommitAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    await dbTransaction.RollbackAsync(cancellationToken);
                    _logger.LogError(ex, "Fallo el deposito {Reference}.", reference);
                    throw;
                }
            }

            _logger.LogInformation("Deposito aprobado. Referencia {Reference}. Monto {Amount}. Cuenta ****{Last}. Cajero {Cashier}",
                reference, amount, target.LastFourDigits, request.Actor.UserId);

            var owner = await _userReadService.GetByIdAsync(target.ClientId);
            var mailOk = await SendAsync(owner?.Email,
                EmailTemplates.DepositSubject(target.LastFourDigits),
                EmailTemplates.DepositBody(owner?.FullName ?? string.Empty, amount, target.LastFourDigits, now),
                cancellationToken);

            return OperationResult.Success(reference, mailOk ? null : AppMessages.DepositOkMailFailed);
        }

        // ================================================================== retiro

        public async Task<OperationResult> WithdrawAsync(WithdrawalRequest request,
            CancellationToken cancellationToken = default)
        {
            var amount = Money.Round(request.Amount);
            if (amount <= 0m)
                return OperationResult.Failure(AppMessages.WithdrawalAmountGreaterThanZero);

            var source = await _accountRepository.GetByAccountNumberAsync(request.SourceAccountNumber);
            if (source == null || !source.IsActive)
                return OperationResult.Failure(AppMessages.InvalidAccountNumber);

            if (source.Balance < amount)
            {
                await RegisterRejectedAsync(source, amount, TransactionType.Debito, TransactionOperation.Retiro,
                    source.AccountNumber, DisplayText.Withdrawal, AppMessages.RejectedInsufficientFunds,
                    request.Actor, cancellationToken);
                return OperationResult.Failure(AppMessages.InsufficientFundsAccount);
            }

            var now = DateTime.Now;
            var reference = Guid.NewGuid().ToString("N");

            await using (var dbTransaction = await _unitOfWork.BeginTransactionAsync(cancellationToken))
            {
                try
                {
                    source.Balance = Money.Round(source.Balance - amount);
                    await _accountRepository.UpdateEntityAsync(source);

                    await _transactionRepository.AddAsync(BuildTransaction(source.Id, amount, TransactionType.Debito,
                        TransactionOperation.Retiro, source.AccountNumber, DisplayText.Withdrawal,
                        reference, request.Actor, now));

                    await dbTransaction.CommitAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    await dbTransaction.RollbackAsync(cancellationToken);
                    _logger.LogError(ex, "Fallo el retiro {Reference}.", reference);
                    throw;
                }
            }

            _logger.LogInformation("Retiro aprobado. Referencia {Reference}. Monto {Amount}. Cuenta ****{Last}. Cajero {Cashier}",
                reference, amount, source.LastFourDigits, request.Actor.UserId);

            var owner = await _userReadService.GetByIdAsync(source.ClientId);
            var mailOk = await SendAsync(owner?.Email,
                EmailTemplates.WithdrawalSubject(source.LastFourDigits),
                EmailTemplates.WithdrawalBody(owner?.FullName ?? string.Empty, amount, source.LastFourDigits, now),
                cancellationToken);

            return OperationResult.Success(reference, mailOk ? null : AppMessages.WithdrawalOkMailFailed);
        }

        // ================================================================== pago a tarjeta

        public async Task<OperationResult> PayCreditCardAsync(CardPaymentRequest request,
            CancellationToken cancellationToken = default)
        {
            var entered = Money.Round(request.Amount);
            if (entered <= 0m)
                return OperationResult.Failure(AppMessages.PaymentAmountGreaterThanZero);

            var source = await _accountRepository.GetByAccountNumberAsync(request.SourceAccountNumber);
            if (source == null || !source.IsActive)
                return OperationResult.Failure(AppMessages.InvalidAccountNumber);

            if (!string.IsNullOrEmpty(request.ExpectedSourceOwnerId) &&
                !string.Equals(source.ClientId, request.ExpectedSourceOwnerId, StringComparison.Ordinal))
                return OperationResult.Failure(AppMessages.InvalidAccountNumber);

            var card = await _creditCardService.GetByIdAsync(request.CreditCardId);
            if (card == null || !card.IsActive)
                return OperationResult.Failure(AppMessages.InvalidCardNumber);

            if (!card.HasDebt)
                return OperationResult.Failure(AppMessages.CardWithoutDebt);

            // Regla anti sobrepago: solo se debita el minimo entre lo digitado y la deuda real.
            var effective = Money.Round(Math.Min(entered, card.Debt));

            if (source.Balance < effective)
            {
                await RegisterRejectedAsync(source, effective, TransactionType.Debito, TransactionOperation.PagoTarjeta,
                    source.AccountNumber, card.LastFourDigits, AppMessages.RejectedInsufficientFunds,
                    request.Actor, cancellationToken);
                return OperationResult.Failure(request.Actor.Role == Roles.Cajero
                    ? AppMessages.InsufficientFundsAccount
                    : AppMessages.InsufficientFundsGeneric);
            }

            var now = DateTime.Now;
            var reference = Guid.NewGuid().ToString("N");

            await using (var dbTransaction = await _unitOfWork.BeginTransactionAsync(cancellationToken))
            {
                try
                {
                    source.Balance = Money.Round(source.Balance - effective);
                    await _accountRepository.UpdateEntityAsync(source);

                    await _transactionRepository.AddAsync(BuildTransaction(source.Id, effective, TransactionType.Debito,
                        TransactionOperation.PagoTarjeta, source.AccountNumber, card.LastFourDigits,
                        reference, request.Actor, now));

                    // Reduce la deuda y actualiza el credito disponible (modulo de tarjetas).
                    await _creditCardService.ApplyPaymentAsync(card.Id, effective, cancellationToken);

                    await dbTransaction.CommitAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    await dbTransaction.RollbackAsync(cancellationToken);
                    _logger.LogError(ex, "Fallo el pago a tarjeta {Reference}. No se aplico ningun movimiento.", reference);
                    throw;
                }
            }

            _logger.LogInformation(
                "Pago a tarjeta aprobado. Referencia {Reference}. Monto efectivo {Amount}. Tarjeta ****{CardLast}. Cuenta ****{AccountLast}",
                reference, effective, card.LastFourDigits, source.LastFourDigits);

            var mailsOk = await NotifyCardPaymentAsync(source, card.ClientId, card.LastFourDigits, effective, now,
                cancellationToken);

            return OperationResult.Success(reference, mailsOk ? null : AppMessages.PaymentOkMailFailed);
        }

        // ================================================================== pago a prestamo

        public async Task<OperationResult> PayLoanAsync(LoanPaymentRequest request,
            CancellationToken cancellationToken = default)
        {
            var entered = Money.Round(request.Amount);
            if (entered <= 0m)
                return OperationResult.Failure(AppMessages.PaymentAmountGreaterThanZero);

            var source = await _accountRepository.GetByAccountNumberAsync(request.SourceAccountNumber);
            if (source == null || !source.IsActive)
                return OperationResult.Failure(AppMessages.InvalidAccountNumber);

            if (!string.IsNullOrEmpty(request.ExpectedSourceOwnerId) &&
                !string.Equals(source.ClientId, request.ExpectedSourceOwnerId, StringComparison.Ordinal))
                return OperationResult.Failure(AppMessages.InvalidAccountNumber);

            var loan = await _loanService.GetByIdAsync(request.LoanId);
            if (loan == null || !loan.IsActive)
                return OperationResult.Failure(AppMessages.InvalidLoanNumber);

            if (!loan.HasPendingInstallments)
                return OperationResult.Failure(AppMessages.LoanWithoutPendingInstallments);

            // Regla anti sobrepago: solo se debita el minimo entre lo digitado y el pendiente real.
            var effective = Money.Round(Math.Min(entered, loan.PendingAmount));

            if (source.Balance < effective)
            {
                await RegisterRejectedAsync(source, effective, TransactionType.Debito, TransactionOperation.PagoPrestamo,
                    source.AccountNumber, loan.LoanNumber, AppMessages.RejectedInsufficientFunds,
                    request.Actor, cancellationToken);
                return OperationResult.Failure(request.Actor.Role == Roles.Cajero
                    ? AppMessages.InsufficientFundsAccount
                    : AppMessages.InsufficientFundsGeneric);
            }

            var now = DateTime.Now;
            var reference = Guid.NewGuid().ToString("N");

            await using (var dbTransaction = await _unitOfWork.BeginTransactionAsync(cancellationToken))
            {
                try
                {
                    source.Balance = Money.Round(source.Balance - effective);
                    await _accountRepository.UpdateEntityAsync(source);

                    await _transactionRepository.AddAsync(BuildTransaction(source.Id, effective, TransactionType.Debito,
                        TransactionOperation.PagoPrestamo, source.AccountNumber, loan.LoanNumber,
                        reference, request.Actor, now));

                    // Aplica el abono en orden de antiguedad sobre la tabla de amortizacion.
                    await _loanService.ApplyPaymentAsync(loan.Id, effective, cancellationToken);

                    await dbTransaction.CommitAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    await dbTransaction.RollbackAsync(cancellationToken);
                    _logger.LogError(ex, "Fallo el pago a prestamo {Reference}. No se aplico ningun movimiento.", reference);
                    throw;
                }
            }

            _logger.LogInformation(
                "Pago a prestamo aprobado. Referencia {Reference}. Monto efectivo {Amount}. Prestamo {Loan}. Cuenta ****{AccountLast}",
                reference, effective, loan.LoanNumber, source.LastFourDigits);

            var owner = await _userReadService.GetByIdAsync(loan.ClientId);
            var mailOk = await SendAsync(owner?.Email,
                EmailTemplates.LoanPaymentSubject(loan.LoanNumber),
                EmailTemplates.LoanPaymentBody(owner?.FullName ?? string.Empty, effective, loan.LoanNumber,
                    source.LastFourDigits, now),
                cancellationToken);

            return OperationResult.Success(reference, mailOk ? null : AppMessages.PaymentOkMailFailed);
        }

        // ================================================================== movimientos externos

        public async Task<OperationResult> RegisterExternalCreditAsync(ExternalCreditRequest request,
            CancellationToken cancellationToken = default)
        {
            var amount = Money.Round(request.Amount);
            if (amount <= 0m) return OperationResult.Failure(AppMessages.TransferAmountGreaterThanZero);

            var target = await _accountRepository.GetByAccountNumberAsync(request.TargetAccountNumber);
            if (target == null || !target.IsActive) return OperationResult.Failure(AppMessages.InvalidAccountNumber);

            var now = DateTime.Now;
            var reference = Guid.NewGuid().ToString("N");

            await using var dbTransaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                target.Balance = Money.Round(target.Balance + amount);
                await _accountRepository.UpdateEntityAsync(target);

                await _transactionRepository.AddAsync(BuildTransaction(target.Id, amount, TransactionType.Credito,
                    request.Operation, request.OriginLabel, target.AccountNumber, reference, request.Actor, now));

                await dbTransaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await dbTransaction.RollbackAsync(cancellationToken);
                throw;
            }

            _logger.LogInformation("{Operation} acreditada en la cuenta ****{Last} por {Amount}. Referencia {Reference}",
                request.Operation, target.LastFourDigits, amount, reference);

            return OperationResult.Success(reference);
        }

        public async Task<OperationResult> RegisterExternalDebitAsync(ExternalDebitRequest request,
            CancellationToken cancellationToken = default)
        {
            var amount = Money.Round(request.Amount);
            if (amount <= 0m) return OperationResult.Failure(AppMessages.TransferAmountGreaterThanZero);

            var source = await _accountRepository.GetByAccountNumberAsync(request.SourceAccountNumber);
            if (source == null || !source.IsActive) return OperationResult.Failure(AppMessages.InvalidAccountNumber);

            if (source.Balance < amount)
            {
                await RegisterRejectedAsync(source, amount, TransactionType.Debito, request.Operation,
                    source.AccountNumber, request.BeneficiaryLabel, AppMessages.RejectedInsufficientFunds,
                    request.Actor, cancellationToken);
                return OperationResult.Failure(AppMessages.InsufficientFundsAccount);
            }

            var now = DateTime.Now;
            var reference = Guid.NewGuid().ToString("N");

            await using var dbTransaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                source.Balance = Money.Round(source.Balance - amount);
                await _accountRepository.UpdateEntityAsync(source);

                await _transactionRepository.AddAsync(BuildTransaction(source.Id, amount, TransactionType.Debito,
                    request.Operation, source.AccountNumber, request.BeneficiaryLabel, reference, request.Actor, now));

                await dbTransaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await dbTransaction.RollbackAsync(cancellationToken);
                throw;
            }

            return OperationResult.Success(reference);
        }

        // ================================================================== consultas

        public async Task<PagedResult<TransactionDto>> GetByAccountAsync(int savingsAccountId, int page, int pageSize,
            CancellationToken cancellationToken = default)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0 || pageSize > PagedResult<TransactionDto>.MaxPageSize)
                pageSize = PagedResult<TransactionDto>.MaxPageSize;

            var query = _transactionRepository.GetByAccountQuery(savingsAccountId)
                .OrderByDescending(t => t.CreatedAt).ThenByDescending(t => t.Id);

            var total = await query.CountAsync(cancellationToken);
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

            return PagedResult<TransactionDto>.Create(_mapper.Map<List<TransactionDto>>(items), page, pageSize, total);
        }

        // ================================================================== helpers

        private static Transaction BuildTransaction(int accountId, decimal amount, TransactionType type,
            TransactionOperation operation, string origin, string beneficiary, string reference,
            OperationActor actor, DateTime moment) => new Transaction
            {
                SavingsAccountId = accountId,
                Amount = amount,
                Type = type,
                Status = TransactionStatus.Aprobada,
                Operation = operation,
                Origin = origin,
                Beneficiary = beneficiary,
                OperationReference = reference,
                PerformedByUserId = actor.UserId,
                PerformedByRole = actor.Role,
                CreatedAt = moment,
                CreatedByUserId = actor.UserId
            };

        /// <summary>
        /// Deja constancia del intento rechazado en la cuenta de origen.
        /// Se persiste fuera de la transaccion de negocio: no toca balances.
        /// </summary>
        private async Task RegisterRejectedAsync(SavingsAccount account, decimal amount, TransactionType type,
            TransactionOperation operation, string origin, string beneficiary, string reason,
            OperationActor actor, CancellationToken cancellationToken)
        {
            await _transactionRepository.AddAsync(new Transaction
            {
                SavingsAccountId = account.Id,
                Amount = Money.Round(amount),
                Type = type,
                Status = TransactionStatus.Rechazada,
                Operation = operation,
                Origin = origin,
                Beneficiary = beneficiary,
                RejectionReason = reason,
                OperationReference = Guid.NewGuid().ToString("N"),
                PerformedByUserId = actor.UserId,
                PerformedByRole = actor.Role,
                CreatedAt = DateTime.Now,
                CreatedByUserId = actor.UserId
            });

            _logger.LogWarning("Intento RECHAZADO ({Reason}) de {Operation} por {Amount} en la cuenta ****{Last}",
                reason, operation, amount, account.LastFourDigits);
        }

        private async Task<bool> NotifyTransferAsync(SavingsAccount source, SavingsAccount target, decimal amount,
            DateTime moment, bool isOwnTransfer, CancellationToken cancellationToken)
        {
            var sender = await _userReadService.GetByIdAsync(source.ClientId);

            if (isOwnTransfer)
            {
                return await SendAsync(sender?.Email, EmailTemplates.TransferSubject,
                    EmailTemplates.TransferBody(sender?.FullName ?? string.Empty, amount,
                        source.LastFourDigits, target.LastFourDigits, moment),
                    cancellationToken);
            }

            var receiver = await _userReadService.GetByIdAsync(target.ClientId);

            var first = await SendAsync(sender?.Email, EmailTemplates.SentSubject(target.LastFourDigits),
                EmailTemplates.SentBody(sender?.FullName ?? string.Empty, amount,
                    source.LastFourDigits, target.LastFourDigits, moment), cancellationToken);

            var second = await SendAsync(receiver?.Email, EmailTemplates.ReceivedSubject(source.LastFourDigits),
                EmailTemplates.ReceivedBody(receiver?.FullName ?? string.Empty, amount,
                    source.LastFourDigits, target.LastFourDigits, moment), cancellationToken);

            return first && second;
        }

        private async Task<bool> NotifyCardPaymentAsync(SavingsAccount source, string cardOwnerId, string cardLastFour,
            decimal amount, DateTime moment, CancellationToken cancellationToken)
        {
            var cardOwner = await _userReadService.GetByIdAsync(cardOwnerId);

            var first = await SendAsync(cardOwner?.Email, EmailTemplates.CardPaymentSubject(cardLastFour),
                EmailTemplates.CardPaymentBody(cardOwner?.FullName ?? string.Empty, amount,
                    source.LastFourDigits, cardLastFour, moment), cancellationToken);

            // Si el duenio de la cuenta origen es distinto al de la tarjeta, tambien se le notifica.
            if (string.Equals(source.ClientId, cardOwnerId, StringComparison.Ordinal)) return first;

            var accountOwner = await _userReadService.GetByIdAsync(source.ClientId);
            var second = await SendAsync(accountOwner?.Email,
                EmailTemplates.CardPaymentDebitNoticeSubject(source.LastFourDigits),
                EmailTemplates.CardPaymentDebitNoticeBody(accountOwner?.FullName ?? string.Empty, amount,
                    source.LastFourDigits, cardLastFour, moment), cancellationToken);

            return first && second;
        }

        /// <summary>Un fallo de correo se registra y se informa, pero nunca revierte la operacion.</summary>
        private async Task<bool> SendAsync(string? to, string subject, string body, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(to)) return false;
            try
            {
                return await _emailService.SendAsync(
                    new EmailRequest { To = to, Subject = subject, HtmlBody = body }, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No fue posible enviar la notificacion '{Subject}'.", subject);
                return false;
            }
        }

        private static string InvalidSourceMessage(TransactionOperation operation)
            => operation == TransactionOperation.TransaccionTerceros
                ? AppMessages.InvalidOriginAccount
                : AppMessages.InvalidAccountNumber;

        private static string InvalidTargetMessage(TransactionOperation operation) => operation switch
        {
            TransactionOperation.TransaccionTerceros => AppMessages.InvalidTargetAccount,
            TransactionOperation.TransaccionBeneficiario => AppMessages.BeneficiaryAccountUnavailable,
            _ => AppMessages.InvalidAccountNumber
        };

        private static string InsufficientFundsMessage(TransactionOperation operation) => operation switch
        {
            TransactionOperation.TransaccionExpress => AppMessages.InsufficientFundsExpress,
            TransactionOperation.TransaccionBeneficiario => AppMessages.InsufficientFundsBeneficiary,
            TransactionOperation.TransferenciaEntreCuentas => AppMessages.InsufficientFundsGeneric,
            _ => AppMessages.InsufficientFundsAccount
        };
    }
}
