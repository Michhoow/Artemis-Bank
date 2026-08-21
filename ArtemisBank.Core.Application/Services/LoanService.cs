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
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ArtemisBank.Core.Application.Services
{
    public class LoanService : GenericService<Loan, LoanDto>, ILoanService
    {
        private readonly ILoanRepository _loanRepository;
        private readonly ILoanInstallmentRepository _installmentRepository;
        private readonly ISavingsAccountRepository _accountRepository;
        private readonly IUserReadService _userReadService;
        private readonly IRiskAssessmentService _riskService;
        private readonly IAccountNumberGenerator _numberGenerator;
        private readonly ITransactionService _transactionService;
        private readonly IEmailService _emailService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<LoanService> _logger;

        public LoanService(
            ILoanRepository loanRepository,
            ILoanInstallmentRepository installmentRepository,
            ISavingsAccountRepository accountRepository,
            IUserReadService userReadService,
            IRiskAssessmentService riskService,
            IAccountNumberGenerator numberGenerator,
            ITransactionService transactionService,
            IEmailService emailService,
            IUnitOfWork unitOfWork,
            IGenericRepository<Loan> genericRepository,
            IMapper mapper,
            ILogger<LoanService> logger)
            : base(genericRepository, mapper)
        {
            _loanRepository = loanRepository;
            _installmentRepository = installmentRepository;
            _accountRepository = accountRepository;
            _userReadService = userReadService;
            _riskService = riskService;
            _numberGenerator = numberGenerator;
            _transactionService = transactionService;
            _emailService = emailService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public override Task<LoanDto?> AddAsync(LoanDto dto)
            => throw new BusinessRuleException(
                "Los préstamos se asignan mediante CreateAsync, que genera la tabla de amortización y el desembolso.");

        public override Task<LoanDto?> UpdateAsync(LoanDto dto, int id)
            => throw new BusinessRuleException(
                "El único cambio permitido sobre un préstamo es la tasa de interés, mediante UpdateRateAsync.");

        public override Task<bool> DeleteAsync(int id)
            => throw new BusinessRuleException(
                "Los préstamos no se eliminan: se completan al saldarse por completo.");

        public async Task<PagedResult<LoanDto>> GetPagedAsync(LoanFilterDto filter,
            CancellationToken cancellationToken = default)
        {
            var page = filter.Page <= 0 ? 1 : filter.Page;
            var pageSize = filter.PageSize is <= 0 or > PagedResult<LoanDto>.MaxPageSize
                ? PagedResult<LoanDto>.MaxPageSize
                : filter.PageSize;

            var status = (filter.Status ?? "activo").Trim().ToLowerInvariant();
            var searchingByIdentification = !string.IsNullOrWhiteSpace(filter.Identification);

            string? clientId = null;
            if (searchingByIdentification)
            {
                var client = await _userReadService.GetByIdentificationAsync(filter.Identification!.Trim());

                if (client == null) return PagedResult<LoanDto>.Empty(page, pageSize);
                clientId = client.Id;
            }

            var query = _loanRepository.QueryWithInstallments().AsNoTracking();

            if (clientId != null) query = query.Where(l => l.ClientId == clientId);

            query = status switch
            {
                "completado" => query.Where(l => l.Status == LoanStatus.Completado),
                "todos" => query,
                _ => query.Where(l => l.Status == LoanStatus.Activo)
            };

            query = searchingByIdentification && status == "todos"
                ? query.OrderBy(l => l.Status).ThenByDescending(l => l.CreatedAt).ThenByDescending(l => l.Id)
                : query.OrderByDescending(l => l.CreatedAt).ThenByDescending(l => l.Id);

            var total = await query.CountAsync(cancellationToken);

            var loans = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var dtos = await MapWithClientAsync(loans);
            return PagedResult<LoanDto>.Create(dtos, page, pageSize, total);
        }

        public async Task<LoanDetailDto?> GetDetailAsync(int loanId,
            CancellationToken cancellationToken = default)
        {
            var loan = await _loanRepository.GetWithInstallmentsAsync(loanId, cancellationToken);
            return loan == null ? null : await MapDetailAsync(loan);
        }

        public async Task<LoanDetailDto?> GetDetailByNumberAsync(string loanNumber,
            CancellationToken cancellationToken = default)
        {
            var loan = await _loanRepository.GetByLoanNumberAsync(loanNumber, cancellationToken);
            return loan == null ? null : await MapDetailAsync(loan);
        }

        public async Task<List<LoanDto>> GetByClientAsync(string clientId, bool onlyActive = true,
            CancellationToken cancellationToken = default)
        {
            var loans = await _loanRepository.GetAllByClientAsync(clientId, cancellationToken);
            if (onlyActive) loans = loans.Where(l => l.Status == LoanStatus.Activo).ToList();

            return await MapWithClientAsync(loans);
        }

        public async Task<HighRiskEvaluationDto> EvaluateRiskAsync(CreateLoanDto request,
            CancellationToken cancellationToken = default)
        {
            ValidateRequestShape(request);

            var plan = AmortizationCalculator.BuildPlan(
                Money.Round(request.CapitalAmount),
                request.AnnualInterestRate,
                request.TermInMonths,
                DateTime.Now);

            return await _riskService.EvaluateAsync(request.ClientId, plan.TotalToPay, cancellationToken);
        }

        public async Task<LoanDetailDto> CreateAsync(CreateLoanDto request, string? adminUserId,
            CancellationToken cancellationToken = default)
        {
            ValidateRequestShape(request);

            var client = await _userReadService.GetByIdAsync(request.ClientId)
                         ?? throw new NotFoundException(AppMessages.LoanClientNotFound);

            if (!client.IsActive)
                throw new BusinessRuleException(AppMessages.LoanOnlyActiveClients);

            if (await _loanRepository.HasActiveLoanAsync(request.ClientId, cancellationToken))
                throw new BusinessRuleException(AppMessages.LoanClientAlreadyHasActiveLoan);

            var principal = await _accountRepository.GetPrincipalByClientAsync(request.ClientId);
            if (principal == null || !principal.IsActive)
                throw new BusinessRuleException(AppMessages.LoanClientNeedsPrincipalAccount);

            var capital = Money.Round(request.CapitalAmount);
            var createdAt = DateTime.Now;

            var plan = AmortizationCalculator.BuildPlan(
                capital, request.AnnualInterestRate, request.TermInMonths, createdAt);

            var risk = await _riskService.EvaluateAsync(request.ClientId, plan.TotalToPay, cancellationToken);
            if (risk.IsHighRisk && !request.ConfirmHighRisk)
                throw new ConflictException(risk.Message);

            var loanNumber = await _numberGenerator.GenerateAsync(cancellationToken);

            var loan = new Loan
            {
                LoanNumber = loanNumber,
                ClientId = request.ClientId,
                ApprovedCapital = capital,
                AnnualInterestRate = request.AnnualInterestRate,
                TermInMonths = request.TermInMonths,
                MonthlyInstallment = plan.MonthlyInstallment,
                TotalToPay = plan.TotalToPay,
                PaidAmount = 0m,
                Status = LoanStatus.Activo,
                DisbursementAccountNumber = principal.AccountNumber,
                FirstDueDate = plan.Rows.First().DueDate,
                CreatedAt = createdAt,
                CreatedByUserId = adminUserId
            };

            await using (var dbTransaction = await _unitOfWork.BeginTransactionAsync(cancellationToken))
            {
                try
                {
                    await _loanRepository.AddAsync(loan);

                    var installments = plan.Rows.Select(row => new LoanInstallment
                    {
                        LoanId = loan.Id,
                        Number = row.Number,
                        DueDate = row.DueDate,
                        TotalAmount = row.TotalAmount,
                        CapitalAmount = row.CapitalAmount,
                        InterestAmount = row.InterestAmount,
                        RemainingCapital = row.RemainingCapital,
                        PaidAmount = 0m,

                        Status = InstallmentStatus.Pendiente,
                        IsOverdue = false,
                        CreatedAt = createdAt,
                        CreatedByUserId = adminUserId
                    }).ToList();

                    await _installmentRepository.AddRangeAsync(installments);

                    var disbursement = await _transactionService.RegisterExternalCreditAsync(
                        new ExternalCreditRequest
                        {
                            TargetAccountNumber = principal.AccountNumber,
                            Amount = capital,
                            Operation = TransactionOperation.DesembolsoPrestamo,
                            OriginLabel = loanNumber,
                            Actor = OperationActor.Of(adminUserId, Roles.Administrador)
                        }, cancellationToken);

                    if (!disbursement.Succeeded)
                        throw new BusinessRuleException(
                            disbursement.ErrorMessage ?? AppMessages.LoanClientNeedsPrincipalAccount);

                    await dbTransaction.CommitAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    await dbTransaction.RollbackAsync(cancellationToken);
                    _logger.LogError(ex,
                        "Fallo la asignacion del prestamo para el cliente {ClientId}. No se aplico ningun movimiento.",
                        request.ClientId);
                    throw;
                }
            }

            _logger.LogInformation(
                "Prestamo {LoanNumber} asignado. Capital {Capital}. Plazo {Term} meses. Tasa {Rate}%. Administrador {AdminId}.",
                loanNumber, capital, request.TermInMonths, request.AnnualInterestRate, adminUserId);

            await TrySendAsync(client.Email,
                EmailTemplates.LoanApprovedSubject(loanNumber),
                EmailTemplates.LoanApprovedBody(client.FullName, loanNumber, capital,
                    plan.MonthlyInstallment, request.TermInMonths, request.AnnualInterestRate,
                    principal.LastFourDigits, createdAt),
                cancellationToken);

            var created = await _loanRepository.GetWithInstallmentsAsync(loan.Id, cancellationToken);
            return await MapDetailAsync(created!);
        }

        public async Task<LoanDetailDto> UpdateRateAsync(int loanId, decimal newAnnualRate, string? adminUserId,
            CancellationToken cancellationToken = default)
        {
            if (newAnnualRate < 0m)
                throw new BusinessRuleException(AppMessages.LoanNegativeRate);

            var loan = await _loanRepository.GetWithInstallmentsAsync(loanId, cancellationToken)
                       ?? throw new NotFoundException(AppMessages.LoanNotFound);

            if (!loan.IsActive)
                throw new BusinessRuleException(AppMessages.LoanNotActive);

            var today = DateTime.Now.Date;
            var all = (loan.Installments ?? new List<LoanInstallment>()).OrderBy(i => i.Number).ToList();

            var future = all
                .Where(i => i.Status == InstallmentStatus.Pendiente
                            && !i.IsOverdue
                            && i.PaidAmount == 0m
                            && i.DueDate.Date > today)
                .OrderBy(i => i.Number)
                .ToList();

            if (future.Count == 0)
                throw new BusinessRuleException(AppMessages.LoanNoFutureInstallments);

            var firstFutureNumber = future.First().Number;
            var previous = all.LastOrDefault(i => i.Number < firstFutureNumber);
            var capitalToReamortize = previous?.RemainingCapital ?? loan.ApprovedCapital;

            var plan = AmortizationCalculator.BuildPlan(
                capitalToReamortize, newAnnualRate, future.Count, future.First().DueDate.AddMonths(-1));

            for (var index = 0; index < future.Count && index < plan.Rows.Count; index++)
            {
                var installment = future[index];
                var row = plan.Rows[index];

                installment.TotalAmount = row.TotalAmount;
                installment.CapitalAmount = row.CapitalAmount;
                installment.InterestAmount = row.InterestAmount;
                installment.RemainingCapital = row.RemainingCapital;

                await _installmentRepository.UpdateEntityAsync(installment);
            }

            loan.AnnualInterestRate = newAnnualRate;
            loan.MonthlyInstallment = plan.MonthlyInstallment;
            loan.TotalToPay = Money.Round(all.Sum(i => i.TotalAmount));

            await _loanRepository.UpdateEntityAsync(loan);

            _logger.LogInformation(
                "Tasa del prestamo {LoanNumber} modificada a {Rate}%. Se recalcularon {Count} cuotas futuras. Administrador {AdminId}.",
                loan.LoanNumber, newAnnualRate, future.Count, adminUserId);

            var client = await _userReadService.GetByIdAsync(loan.ClientId);
            await TrySendAsync(client?.Email,
                EmailTemplates.LoanRateChangedSubject(loan.LoanNumber),
                EmailTemplates.LoanRateChangedBody(client?.FullName ?? string.Empty, loan.LoanNumber,
                    newAnnualRate, plan.MonthlyInstallment, future.Count, DateTime.Now),
                cancellationToken);

            var updated = await _loanRepository.GetWithInstallmentsAsync(loanId, cancellationToken);
            return await MapDetailAsync(updated!);
        }

        private static void ValidateRequestShape(CreateLoanDto request)
        {
            if (string.IsNullOrWhiteSpace(request.ClientId))
                throw new BusinessRuleException(AppMessages.LoanClientRequired);

            if (request.CapitalAmount <= 0m)
                throw new BusinessRuleException(AppMessages.LoanAmountGreaterThanZero);

            if (request.AnnualInterestRate < 0m)
                throw new BusinessRuleException(AppMessages.LoanNegativeRate);

            if (!AmortizationCalculator.IsAllowedTerm(request.TermInMonths))
                throw new BusinessRuleException(AppMessages.LoanInvalidTerm);
        }

        private async Task<List<LoanDto>> MapWithClientAsync(List<Loan> loans)
        {
            if (loans.Count == 0) return new List<LoanDto>();

            var owners = await _userReadService.GetByIdsAsync(loans.Select(l => l.ClientId).Distinct());
            var byId = owners.ToDictionary(o => o.Id, StringComparer.Ordinal);

            return loans.Select(loan =>
            {
                byId.TryGetValue(loan.ClientId, out var owner);
                return MapBasic(loan, owner);
            }).ToList();
        }

        private async Task<LoanDetailDto> MapDetailAsync(Loan loan)
        {
            var owner = await _userReadService.GetByIdAsync(loan.ClientId);
            var basic = MapBasic(loan, owner);

            var detail = new LoanDetailDto
            {
                Id = basic.Id,
                LoanNumber = basic.LoanNumber,
                ClientId = basic.ClientId,
                ClientFullName = basic.ClientFullName,
                ClientIdentification = basic.ClientIdentification,
                CapitalAmount = basic.CapitalAmount,
                MonthlyInstallment = basic.MonthlyInstallment,
                TotalToPay = basic.TotalToPay,
                PaidAmount = basic.PaidAmount,
                PendingAmount = basic.PendingAmount,
                TotalInstallments = basic.TotalInstallments,
                PaidInstallments = basic.PaidInstallments,
                AnnualInterestRate = basic.AnnualInterestRate,
                TermInMonths = basic.TermInMonths,
                Status = basic.Status,
                ClientPaymentStatus = basic.ClientPaymentStatus,
                DisbursementAccountNumber = basic.DisbursementAccountNumber,
                CreatedAt = basic.CreatedAt,
                Installments = (loan.Installments ?? new List<LoanInstallment>())
                    .OrderBy(i => i.Number)
                    .Select(MapInstallment)
                    .ToList()
            };

            return detail;
        }

        private static LoanDto MapBasic(Loan loan, UserInfoDto? owner) => new LoanDto
        {
            Id = loan.Id,
            LoanNumber = loan.LoanNumber,
            ClientId = loan.ClientId,
            ClientFullName = owner?.FullName ?? string.Empty,
            ClientIdentification = owner?.Identification ?? string.Empty,
            CapitalAmount = loan.ApprovedCapital,
            MonthlyInstallment = loan.MonthlyInstallment,
            TotalToPay = loan.TotalToPay,
            PaidAmount = loan.PaidAmount,
            PendingAmount = Money.Round(loan.PendingAmount),
            TotalInstallments = loan.TotalInstallments,
            PaidInstallments = loan.PaidInstallments,
            AnnualInterestRate = loan.AnnualInterestRate,
            TermInMonths = loan.TermInMonths,
            Status = loan.Status.ToString(),
            ClientPaymentStatus = loan.IsOverdue ? DisplayText.LoanOverdue : DisplayText.LoanUpToDate,
            DisbursementAccountNumber = loan.DisbursementAccountNumber,
            CreatedAt = loan.CreatedAt
        };

        private static LoanInstallmentDto MapInstallment(LoanInstallment installment) => new LoanInstallmentDto
        {
            Id = installment.Id,
            Number = installment.Number,
            DueDate = installment.DueDate,
            TotalAmount = installment.TotalAmount,
            CapitalAmount = installment.CapitalAmount,
            InterestAmount = installment.InterestAmount,
            PaidAmount = installment.PaidAmount,
            PendingAmount = Money.Round(installment.PendingAmount),
            RemainingCapital = installment.RemainingCapital,
            PaymentStatus = installment.Status switch
            {
                InstallmentStatus.Pagada => DisplayText.InstallmentPaid,
                InstallmentStatus.ParcialmentePagada => DisplayText.InstallmentPartiallyPaid,
                _ => DisplayText.InstallmentPending
            },
            IsOverdue = installment.IsOverdue && !installment.IsSettled,
            PaidAt = installment.PaidAt
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
                _logger.LogWarning(ex, "No fue posible enviar una notificacion del modulo de prestamos.");
                return false;
            }
        }
    }
}
