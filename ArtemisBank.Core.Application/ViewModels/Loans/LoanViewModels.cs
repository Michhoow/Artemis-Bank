using System.ComponentModel.DataAnnotations;
using ArtemisBank.Core.Application.Common.Constants;
using ArtemisBank.Core.Application.Common.Models;
using ArtemisBank.Core.Application.Dtos.Loans;

namespace ArtemisBank.Core.Application.ViewModels.Loans
{
    public class LoanIndexViewModel
    {
        public PagedResult<LoanDto> Loans { get; set; } = PagedResult<LoanDto>.Empty();
        public string? Identification { get; set; }
        public string Status { get; set; } = "activo";
        public string? InfoMessage { get; set; }
    }

    public class AssignLoanViewModel
    {
        [Required(ErrorMessage = AppMessages.LoanClientRequired)]
        [Display(Name = "Cliente")]
        public string ClientId { get; set; } = string.Empty;

        [Display(Name = "Cliente")]
        public string ClientDisplay { get; set; } = string.Empty;

        [Required(ErrorMessage = AppMessages.LoanAmountGreaterThanZero)]
        [Range(0.01, 999999999.99, ErrorMessage = AppMessages.LoanAmountGreaterThanZero)]
        [Display(Name = "Monto a prestar")]
        public decimal CapitalAmount { get; set; }

        [Required(ErrorMessage = AppMessages.LoanInvalidTerm)]
        [Display(Name = "Plazo del préstamo (meses)")]
        public int TermInMonths { get; set; } = 12;

        [Required(ErrorMessage = "La tasa de interés anual es requerida.")]
        [Range(0, 100, ErrorMessage = AppMessages.LoanNegativeRate)]
        [Display(Name = "Tasa de interés anual (%)")]
        public decimal AnnualInterestRate { get; set; }

        public bool ConfirmHighRisk { get; set; }

        public IReadOnlyList<int> AllowedTerms => AmortizationCalculator.AllowedTerms;
    }

    public class SelectLoanClientViewModel
    {
        [Display(Name = "Cédula del cliente")]
        public string? Identification { get; set; }

        public List<ClientOptionViewModel> Clients { get; set; } = new();
        public string? InfoMessage { get; set; }
    }

    public class ClientOptionViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Identification { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool HasActiveLoan { get; set; }
        public bool HasPrincipalAccount { get; set; }
    }

    public class HighRiskWarningViewModel
    {
        public HighRiskEvaluationDto Evaluation { get; set; } = new();
        public AssignLoanViewModel Request { get; set; } = new();

        public string CurrentDebtDisplay => Money.Format(Evaluation.CurrentDebt);
        public string ProjectedDebtDisplay => Money.Format(Evaluation.ProjectedDebt);
        public string AverageDebtDisplay => Money.Format(Evaluation.AverageDebt);
    }

    public class EditLoanRateViewModel
    {
        public int LoanId { get; set; }
        public string LoanNumber { get; set; } = string.Empty;
        public string ClientFullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "La tasa de interés anual es requerida.")]
        [Range(0, 100, ErrorMessage = AppMessages.LoanNegativeRate)]
        [Display(Name = "Tasa de interés anual (%)")]
        public decimal AnnualInterestRate { get; set; }
    }
}
