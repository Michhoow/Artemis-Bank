using ArtemisBank.Core.Application.Common.Models;

namespace ArtemisBank.Core.Application.Dtos.Loans
{
    public class LoanDto
    {
        public int Id { get; set; }
        public string LoanNumber { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string ClientFullName { get; set; } = string.Empty;
        public string ClientIdentification { get; set; } = string.Empty;

        public decimal CapitalAmount { get; set; }
        public decimal MonthlyInstallment { get; set; }
        public decimal TotalToPay { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal PendingAmount { get; set; }

        public int TotalInstallments { get; set; }
        public int PaidInstallments { get; set; }
        public decimal AnnualInterestRate { get; set; }
        public int TermInMonths { get; set; }

        public string Status { get; set; } = string.Empty;

        public string ClientPaymentStatus { get; set; } = string.Empty;

        public string DisbursementAccountNumber { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        public string CapitalAmountDisplay => Money.Format(CapitalAmount);
        public string PendingAmountDisplay => Money.Format(PendingAmount);
        public string MonthlyInstallmentDisplay => Money.Format(MonthlyInstallment);
        public string RateDisplay => Rate.Format(AnnualInterestRate);
    }

    public class LoanInstallmentDto
    {
        public int Id { get; set; }
        public int Number { get; set; }
        public DateTime DueDate { get; set; }

        public decimal TotalAmount { get; set; }
        public decimal CapitalAmount { get; set; }
        public decimal InterestAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal PendingAmount { get; set; }
        public decimal RemainingCapital { get; set; }

        public string PaymentStatus { get; set; } = string.Empty;

        public bool IsOverdue { get; set; }

        public DateTime? PaidAt { get; set; }

        public string DueDateDisplay => DueDate.ToString("dd/MM/yyyy");
        public string TotalAmountDisplay => Money.Format(TotalAmount);
        public string PendingAmountDisplay => Money.Format(PendingAmount);
        public string OverdueDisplay => IsOverdue ? "Atrasada" : "Al día";
    }

    public class LoanDetailDto : LoanDto
    {
        public List<LoanInstallmentDto> Installments { get; set; } = new();
    }

    public class LoanFilterDto
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;

        public string? Identification { get; set; }

        public string? Status { get; set; }
    }

    public class CreateLoanDto
    {
        public string ClientId { get; set; } = string.Empty;
        public decimal CapitalAmount { get; set; }
        public int TermInMonths { get; set; }
        public decimal AnnualInterestRate { get; set; }

        public bool ConfirmHighRisk { get; set; }
    }

    public class UpdateLoanRateDto
    {
        public decimal AnnualInterestRate { get; set; }
    }

    public class HighRiskEvaluationDto
    {
        public bool IsHighRisk { get; set; }

        public string RiskType { get; set; } = "None";
        public string Message { get; set; } = string.Empty;
        public decimal CurrentDebt { get; set; }
        public decimal ProjectedDebt { get; set; }
        public decimal AverageDebt { get; set; }
    }
}
