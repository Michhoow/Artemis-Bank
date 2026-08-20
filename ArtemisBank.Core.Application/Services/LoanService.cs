using ArtemisBank.Core.Application.Common.Constants;
using ArtemisBank.Core.Application.Common.Exceptions;
using ArtemisBank.Core.Application.Common.Models;
using ArtemisBank.Core.Application.Dtos.Common;
using ArtemisBank.Core.Application.Dtos.Loans;
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
    /// Reglas de negocio de prestamos. Dueno: Manuel.
    ///
    /// Ciclo completo de asignacion:
    ///   validar → evaluar riesgo (deuda promedio) → generar amortizacion francesa →
    ///   desembolsar a la cuenta principal (reutilizando la trazabilidad de Michael) → notificar.
    ///
    /// El desembolso, la creacion del prestamo y las cuotas ocurren dentro de UNA transaccion:
    /// si algo falla, no queda un prestamo a medias ni un credito sin prestamo.
    /// </summary>
    public class LoanService : ILoanService
    {
        private readonly ILoanRepository _loanRepository;
        private readonly ICreditCardRepository _creditCardRepository;
        private readonly IUserReadService _userReadService;
        private readonly ISavingsAccountService _savingsAccountService;
        private readonly ITransactionService _transactionService;
        private readonly IAmortizationService _amortization;
        private readonly IAccountNumberGenerator _numberGenerator;
        private readonly IEmailService _emailService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<LoanService> _logger;

        private static readonly int[] AllowedTerms = { 6, 12, 18, 24, 30, 36, 42, 48, 54, 60 };

        public LoanService(
            ILoanRepository loanRepository,
            ICreditCardRepository creditCardRepository,
            IUserReadService userReadService,
            ISavingsAccountService savingsAccountService,
            ITransactionService transactionService,
            IAmortizationService amortization,
            IAccountNumberGenerator numberGenerator,
            IEmailService emailService,
            IUnitOfWork unitOfWork,
            ILogger<LoanService> logger)
        {
            _loanRepository = loanRepository;
            _creditCardRepository = creditCardRepository;
            _userReadService = userReadService;
            _savingsAccountService = savingsAccountService;
            _transactionService = transactionService;
            _amortization = amortization;
            _numberGenerator = numberGenerator;
            _emailService = emailService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        // ================================================================== listados

        public async Task<PagedResult<LoanListItemDto>> GetPagedAsync(LoanFilterDto filter,
            CancellationToken cancellationToken = default)
            => (await SearchAsync(filter, cancellationToken)).Result;

        public async Task<(PagedResult<LoanListItemDto> Result, string? InfoMessage)> SearchAsync(
            LoanFilterDto filter, CancellationToken cancellationToken = default)
        {
            var page = filter.Page <= 0 ? 1 : filter.Page;
            var pageSize = NormalizePageSize(filter.PageSize);
            var status = NormalizeLoanStatus(filter.Status);

            var query = _loanRepository.QueryWithInstallments();
            string? infoMessage = null;
            var searchingByIdentification = !string.IsNullOrWhiteSpace(filter.Identification);

            UserInfoDto? client = null;
            if (searchingByIdentification)
            {
                client = await _userReadService.GetByIdentificationAsync(filter.Identification!.Trim());
                if (client == null)
                    return (PagedResult<LoanListItemDto>.Empty(page, pageSize),
                        AppMessages.ClientNotFoundByIdentification);

                query = query.Where(l => l.ClientId == client.Id);
            }

            if (status == "activos") query = query.Where(l => l.Status == LoanStatus.Activo);
            else if (status == "completados") query = query.Where(l => l.Status == LoanStatus.Completado);

            var loans = await query.ToListAsync(cancellationToken);

            // Al buscar por cedula sin estado: primero activos, luego completados; dentro, del mas reciente al mas antiguo.
            if (searchingByIdentification && string.IsNullOrWhiteSpace(filter.Status))
                loans = loans
                    .OrderBy(l => l.Status == LoanStatus.Activo ? 0 : 1)
                    .ThenByDescending(l => l.CreatedAt)
                    .ToList();
            else
                loans = loans.OrderByDescending(l => l.CreatedAt).ToList();

            if (searchingByIdentification && loans.Count == 0)
                infoMessage = AppMessages.ClientHasNoLoans;

            var total = loans.Count;
            var pageItems = loans.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            var names = await ResolveNamesAsync(pageItems.Select(l => l.ClientId));
            var data = pageItems.Select(l => ToListItem(l, names)).ToList();

            return (PagedResult<LoanListItemDto>.Create(data, page, pageSize, total), infoMessage);
        }

        public async Task<LoanDetailDto?> GetDetailAsync(int loanId, CancellationToken cancellationToken = default)
        {
            var loan = await _loanRepository.GetByIdWithInstallmentsAsync(loanId, cancellationToken);
            if (loan == null) return null;

            var client = await _userReadService.GetByIdAsync(loan.ClientId);
            var ordered = loan.Installments.OrderBy(i => i.InstallmentNumber).ToList();

            return new LoanDetailDto
            {
                Id = loan.Id,
                LoanNumber = loan.LoanNumber,
                ClientId = loan.ClientId,
                ClientFullName = client?.FullName ?? string.Empty,
                CapitalAmount = loan.ApprovedCapital,
                AnnualInterestRate = loan.AnnualInterestRate,
                TermInMonths = loan.TermInMonths,
                MonthlyInstallment = ordered.FirstOrDefault()?.InstallmentAmount ?? 0m,
                PendingAmount = loan.PendingAmount,
                Status = LoanStatusText(loan.Status),
                ClientPaymentStatus = loan.IsOverdue ? "En mora" : "Al día",
                CreatedAt = loan.CreatedAt,
                Amortization = ordered.Select(ToInstallmentDto).ToList()
            };
        }

        public async Task<List<UserInfoDto>> GetEligibleClientsAsync(string? identification,
            CancellationToken cancellationToken = default)
        {
            var clients = await _userReadService.GetClientsAsync(onlyActive: true);

            if (!string.IsNullOrWhiteSpace(identification))
            {
                var needle = identification.Trim();
                clients = clients.Where(c => c.Identification.Contains(needle)).ToList();
            }

            // Excluye clientes que ya tienen un prestamo activo.
            var eligible = new List<UserInfoDto>();
            foreach (var client in clients)
            {
                if (!await _loanRepository.HasActiveLoanAsync(client.Id, cancellationToken))
                    eligible.Add(client);
            }

            return eligible;
        }

        // ================================================================== deuda promedio / riesgo

        public async Task<decimal> GetAverageActiveClientDebtAsync(CancellationToken cancellationToken = default)
        {
            var activeClientIds = await _userReadService.GetActiveClientIdsAsync();
            if (activeClientIds.Count == 0) return 0m; // sin clientes activos ⇒ promedio RD$0.00

            decimal total = 0m;
            foreach (var clientId in activeClientIds)
                total += await GetClientTotalDebtAsync(clientId, cancellationToken);

            return Money.Round(total / activeClientIds.Count);
        }

        private async Task<decimal> GetClientTotalDebtAsync(string clientId, CancellationToken cancellationToken)
        {
            var loanDebt = await _loanRepository.GetActivePendingDebtByClientAsync(clientId, cancellationToken);
            var cardDebt = await _creditCardRepository.GetActiveDebtByClientAsync(clientId, cancellationToken);
            return Money.Round(loanDebt + cardDebt);
        }

        // ================================================================== asignacion

        public async Task<LoanCreatedDto> AssignAsync(AssignLoanDto request,
            CancellationToken cancellationToken = default)
        {
            // ---- Validaciones de negocio (las estructurales las cubre FluentValidation en el command).
            var capital = Money.Round(request.CapitalAmount);
            if (capital <= 0m) throw new BusinessRuleException(AppMessages.LoanAmountGreaterThanZero);
            if (request.AnnualInterestRate < 0m) throw new BusinessRuleException(AppMessages.NegativeInterestRate);
            if (!AllowedTerms.Contains(request.TermInMonths))
                throw new BusinessRuleException(AppMessages.InvalidLoanTerm);

            var client = await _userReadService.GetByIdAsync(request.ClientId)
                ?? throw new NotFoundException(AppMessages.ClientNotFoundByIdentification);

            if (!client.IsActive) throw new BusinessRuleException(AppMessages.OnlyActiveClients);

            if (await _loanRepository.HasActiveLoanAsync(client.Id, cancellationToken))
                throw new BusinessRuleException(AppMessages.ClientAlreadyHasActiveLoan);

            var principal = await _savingsAccountService.GetActiveByClientAsync(client.Id, cancellationToken);
            var principalAccount = principal.FirstOrDefault(a => a.Type == "Principal");
            if (principalAccount == null)
                throw new BusinessRuleException(AppMessages.ClientNeedsPrincipalForDisbursement);

            // ---- Amortizacion (para conocer el total a pagar del nuevo prestamo).
            var now = DateTime.Now;
            var firstDueDate = now.Date.AddMonths(1);
            var schedule = _amortization.BuildSchedule(capital, request.AnnualInterestRate,
                request.TermInMonths, firstDueDate);
            var totalToPay = Money.Round(schedule.Sum(s => s.InstallmentAmount));
            var monthlyInstallment = schedule.FirstOrDefault()?.InstallmentAmount ?? 0m;

            // ---- Evaluacion de riesgo.
            await EvaluateRiskOrThrowAsync(client.Id, totalToPay, request.ConfirmHighRisk, cancellationToken);

            // ---- Persistencia + desembolso, todo atomico.
            var loanNumber = await _numberGenerator.GenerateAsync(cancellationToken);
            var loan = new Loan
            {
                LoanNumber = loanNumber,
                ClientId = client.Id,
                ApprovedCapital = capital,
                AnnualInterestRate = request.AnnualInterestRate,
                TermInMonths = request.TermInMonths,
                Status = LoanStatus.Activo,
                AssignedByUserId = request.AssignedByUserId,
                CreatedByUserId = request.AssignedByUserId,
                CreatedAt = now,
                Installments = schedule.Select(s => new Installment
                {
                    InstallmentNumber = s.InstallmentNumber,
                    DueDate = s.DueDate,
                    InstallmentAmount = s.InstallmentAmount,
                    InterestAmount = s.InterestAmount,
                    CapitalAmount = s.CapitalAmount,
                    PendingAmount = s.InstallmentAmount,
                    Status = InstallmentStatus.Pendiente,
                    IsLate = false,
                    CreatedAt = now,
                    CreatedByUserId = request.AssignedByUserId
                }).ToList()
            };

            await using (var dbTransaction = await _unitOfWork.BeginTransactionAsync(cancellationToken))
            {
                try
                {
                    await _loanRepository.AddAsync(loan);

                    // Desembolso: CREDITO en la cuenta principal reutilizando la trazabilidad de Michael.
                    var credit = await _transactionService.RegisterExternalCreditAsync(new ExternalCreditRequest
                    {
                        TargetAccountNumber = principalAccount.AccountNumber,
                        Amount = capital,
                        Operation = TransactionOperation.DesembolsoPrestamo,
                        OriginLabel = loanNumber,
                        Actor = OperationActor.Of(request.AssignedByUserId, Roles.Administrador)
                    }, cancellationToken);

                    if (!credit.Succeeded)
                        throw new BusinessRuleException(credit.ErrorMessage ?? AppMessages.ClientNeedsPrincipalForDisbursement);

                    await dbTransaction.CommitAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    await dbTransaction.RollbackAsync(cancellationToken);
                    _logger.LogError(ex, "Fallo la asignacion del prestamo {LoanNumber}. No se aplico ningun movimiento.",
                        loanNumber);
                    throw;
                }
            }

            _logger.LogInformation(
                "Prestamo {LoanNumber} creado para el cliente {ClientId}. Capital {Capital}. Plazo {Term}m. Cuota {Installment}.",
                loanNumber, client.Id, capital, request.TermInMonths, monthlyInstallment);

            // El correo va FUERA de la transaccion: un fallo de correo no revierte el prestamo.
            var mailOk = await _emailService.SendAsync(new EmailRequest
            {
                To = client.Email,
                Subject = EmailTemplates.LoanApprovedSubject,
                HtmlBody = EmailTemplates.LoanApprovedBody(client.FullName, capital, request.TermInMonths,
                    request.AnnualInterestRate, monthlyInstallment, loanNumber)
            }, cancellationToken);

            if (!mailOk)
                _logger.LogWarning("El prestamo {LoanNumber} se creo, pero fallo el correo de notificacion.", loanNumber);

            return new LoanCreatedDto
            {
                Id = loan.Id,
                LoanNumber = loanNumber,
                ClientId = client.Id,
                ClientFullName = client.FullName,
                CapitalAmount = capital,
                TermInMonths = request.TermInMonths,
                AnnualInterestRate = request.AnnualInterestRate,
                MonthlyInstallment = monthlyInstallment,
                TotalAmountToPay = totalToPay,
                Status = LoanStatusText(LoanStatus.Activo),
                CreatedAt = now
            };
        }

        private async Task EvaluateRiskOrThrowAsync(string clientId, decimal totalToPay, bool confirmHighRisk,
            CancellationToken cancellationToken)
        {
            var averageDebt = await GetAverageActiveClientDebtAsync(cancellationToken);
            var currentDebt = await GetClientTotalDebtAsync(clientId, cancellationToken);
            var projectedDebt = Money.Round(currentDebt + totalToPay);

            var currentHighRisk = currentDebt > averageDebt;
            var projectedHighRisk = projectedDebt > averageDebt;

            if (!currentHighRisk && !projectedHighRisk) return; // sin riesgo: se crea sin advertencia
            if (confirmHighRisk) return;                        // confirmado: se permite

            var riskType = currentHighRisk ? "CurrentHighRisk" : "ProjectedHighRisk";
            var message = currentHighRisk ? AppMessages.CurrentHighRisk : AppMessages.ProjectedHighRisk;

            throw new HighRiskConflictException(new HighRiskConflictDto
            {
                Message = message,
                RiskType = riskType,
                CurrentDebt = currentDebt,
                ProjectedDebt = projectedDebt,
                AverageDebt = averageDebt
            });
        }

        // ================================================================== edicion de tasa

        public async Task UpdateRateAsync(int loanId, decimal newAnnualRate, CancellationToken cancellationToken = default)
        {
            if (newAnnualRate < 0m) throw new BusinessRuleException(AppMessages.NegativeInterestRate);

            var loan = await _loanRepository.GetByIdWithInstallmentsAsync(loanId, cancellationToken)
                ?? throw new NotFoundException(AppMessages.LoanDoesNotExist);

            if (!loan.IsActive) throw new BusinessRuleException(AppMessages.OnlyActiveLoanRateEditable);

            var today = DateTime.Now.Date;

            // Solo cuotas futuras pendientes: no pagadas, no vencidas, no parciales, vencimiento posterior a hoy.
            var futureInstallments = loan.Installments
                .Where(i => i.Status == InstallmentStatus.Pendiente
                            && !i.IsLate
                            && i.DueDate.Date > today)
                .OrderBy(i => i.InstallmentNumber)
                .ToList();

            if (futureInstallments.Count == 0)
                throw new BusinessRuleException(AppMessages.NoFutureInstallmentsToRecalculate);

            // El capital residual a redistribuir es la suma del CAPITAL de las cuotas futuras.
            var residualCapital = Money.Round(futureInstallments.Sum(i => i.CapitalAmount));

            var recalculated = _amortization.RecalculateFutureInstallments(
                residualCapital, newAnnualRate, futureInstallments.Count,
                futureInstallments[0].DueDate, futureInstallments[0].InstallmentNumber);

            for (var i = 0; i < futureInstallments.Count; i++)
            {
                var installment = futureInstallments[i];
                var line = recalculated[i];
                installment.InstallmentAmount = line.InstallmentAmount;
                installment.InterestAmount = line.InterestAmount;
                installment.CapitalAmount = line.CapitalAmount;
                installment.PendingAmount = line.InstallmentAmount; // aun no se ha pagado nada de ella
            }

            loan.AnnualInterestRate = newAnnualRate;
            await _loanRepository.UpdateEntityAsync(loan);

            _logger.LogInformation("Tasa del prestamo {LoanNumber} actualizada a {Rate}%. {Count} cuotas recalculadas.",
                loan.LoanNumber, newAnnualRate, futureInstallments.Count);

            var client = await _userReadService.GetByIdAsync(loan.ClientId);
            var mailOk = await _emailService.SendAsync(new EmailRequest
            {
                To = client?.Email ?? string.Empty,
                Subject = EmailTemplates.LoanRateUpdatedSubject,
                HtmlBody = EmailTemplates.LoanRateUpdatedBody(client?.FullName ?? string.Empty, loan.LoanNumber,
                    newAnnualRate, recalculated.First().InstallmentAmount)
            }, cancellationToken);

            if (!mailOk)
                _logger.LogWarning("Se actualizo la tasa del prestamo {LoanNumber}, pero fallo el correo.", loan.LoanNumber);
        }

        // ================================================================== proceso diario de mora

        public async Task<int> RefreshOverdueInstallmentsAsync(DateTime asOf, CancellationToken cancellationToken = default)
        {
            var candidates = await _loanRepository.GetOverdueCandidateInstallmentsAsync(asOf, cancellationToken);
            var changed = 0;

            foreach (var installment in candidates)
            {
                var shouldBeLate = installment.DueDate.Date < asOf.Date
                                   && installment.Status != InstallmentStatus.Pagada;

                if (installment.IsLate != shouldBeLate)
                {
                    installment.IsLate = shouldBeLate;
                    changed++;
                }
            }

            if (changed > 0)
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Proceso de mora: {Count} cuotas actualizadas al {AsOf:yyyy-MM-dd}.", changed, asOf);
            }

            return changed;
        }

        // ================================================================== helpers

        private static int NormalizePageSize(int pageSize)
        {
            if (pageSize <= 0) return PagedResult<LoanListItemDto>.MaxPageSize;
            return pageSize > PagedResult<LoanListItemDto>.MaxPageSize
                ? PagedResult<LoanListItemDto>.MaxPageSize
                : pageSize;
        }

        private static string NormalizeLoanStatus(string? status)
        {
            var value = (status ?? "activos").Trim().ToLowerInvariant();
            return value is "activos" or "completados" or "todos" ? value : "activos";
        }

        private static string LoanStatusText(LoanStatus status) => status == LoanStatus.Activo ? "Activo" : "Completado";

        private async Task<Dictionary<string, string>> ResolveNamesAsync(IEnumerable<string> clientIds)
        {
            var ids = clientIds.Distinct().ToList();
            if (ids.Count == 0) return new Dictionary<string, string>();

            var users = await _userReadService.GetByIdsAsync(ids);
            return users.ToDictionary(u => u.Id, u => u.FullName);
        }

        private static LoanListItemDto ToListItem(Loan loan, IReadOnlyDictionary<string, string> names)
            => new LoanListItemDto
            {
                Id = loan.Id,
                LoanNumber = loan.LoanNumber,
                ClientId = loan.ClientId,
                ClientFullName = names.TryGetValue(loan.ClientId, out var name) ? name : string.Empty,
                CapitalAmount = loan.ApprovedCapital,
                TotalInstallments = loan.TotalInstallments,
                PaidInstallments = loan.PaidInstallments,
                PendingAmount = loan.PendingAmount,
                AnnualInterestRate = loan.AnnualInterestRate,
                TermInMonths = loan.TermInMonths,
                Status = LoanStatusText(loan.Status),
                ClientPaymentStatus = loan.IsOverdue ? "En mora" : "Al día",
                CreatedAt = loan.CreatedAt
            };

        private static InstallmentDto ToInstallmentDto(Installment i) => new InstallmentDto
        {
            InstallmentNumber = i.InstallmentNumber,
            DueDate = i.DueDate,
            InstallmentAmount = i.InstallmentAmount,
            InterestAmount = i.InterestAmount,
            CapitalAmount = i.CapitalAmount,
            PendingInstallmentAmount = i.PendingAmount,
            PaymentStatus = InstallmentStatusText(i.Status),
            IsLate = i.IsLate
        };

        private static string InstallmentStatusText(InstallmentStatus status) => status switch
        {
            InstallmentStatus.Pagada => "Pagada",
            InstallmentStatus.ParcialmentePagada => "Parcialmente pagada",
            _ => "Pendiente"
        };
    }
}
