using System.ComponentModel.DataAnnotations;
using ArtemisBank.Core.Application.Common.Constants;
using ArtemisBank.Core.Application.Common.Models;
using ArtemisBank.Core.Application.Dtos.CreditCards;
using ArtemisBank.Core.Application.Dtos.SavingsAccounts;
using ArtemisBank.Core.Application.ViewModels.Loans;

namespace ArtemisBank.Core.Application.ViewModels.CreditCards
{
    public class CreditCardIndexViewModel
    {
        public PagedResult<CreditCardDto> Cards { get; set; } = PagedResult<CreditCardDto>.Empty();
        public string? Identification { get; set; }
        public string Status { get; set; } = "activa";
        public string? InfoMessage { get; set; }
    }

    public class AssignCreditCardViewModel
    {
        [Required(ErrorMessage = AppMessages.CardClientRequired)]
        [Display(Name = "Cliente")]
        public string ClientId { get; set; } = string.Empty;

        [Display(Name = "Cliente")]
        public string ClientDisplay { get; set; } = string.Empty;

        [Required(ErrorMessage = AppMessages.CardLimitGreaterThanZero)]
        [Range(0.01, 999999999.99, ErrorMessage = AppMessages.CardLimitGreaterThanZero)]
        [Display(Name = "Límite de crédito")]
        public decimal CreditLimit { get; set; }
    }

    public class SelectCardClientViewModel
    {
        [Display(Name = "Cédula del cliente")]
        public string? Identification { get; set; }

        public List<ClientOptionViewModel> Clients { get; set; } = new();
        public string? InfoMessage { get; set; }
    }

    public class CreatedCardViewModel
    {
        public CreditCardDto Card { get; set; } = new();
        public string Cvc { get; set; } = string.Empty;
    }

    public class EditCardLimitViewModel
    {
        public int CreditCardId { get; set; }
        public string MaskedNumber { get; set; } = string.Empty;
        public string ClientFullName { get; set; } = string.Empty;
        public decimal CurrentDebt { get; set; }

        [Required(ErrorMessage = AppMessages.CardLimitGreaterThanZero)]
        [Range(0.01, 999999999.99, ErrorMessage = AppMessages.CardLimitGreaterThanZero)]
        [Display(Name = "Límite de crédito")]
        public decimal CreditLimit { get; set; }

        public string CurrentDebtDisplay => Money.Format(CurrentDebt);
    }

    public class CancelCardViewModel
    {
        public int CreditCardId { get; set; }
        public string MaskedNumber { get; set; } = string.Empty;
        public string ClientFullName { get; set; } = string.Empty;
        public decimal Debt { get; set; }
        public bool CanCancel => Debt <= 0m;
        public string DebtDisplay => Money.Format(Debt);
    }

    public class CashAdvanceViewModel
    {
        [Required(ErrorMessage = "Debe seleccionar una tarjeta de crédito.")]
        [Display(Name = "Tarjeta de crédito")]
        public int CreditCardId { get; set; }

        [Required(ErrorMessage = AppMessages.AdvanceTargetAccountRequired)]
        [Display(Name = "Cuenta de destino")]
        public string TargetAccountNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = AppMessages.AdvanceAmountGreaterThanZero)]
        [Range(0.01, 999999999.99, ErrorMessage = AppMessages.AdvanceAmountGreaterThanZero)]
        [Display(Name = "Monto del avance")]
        public decimal Amount { get; set; }

        public List<CreditCardDto> Cards { get; set; } = new();
        public List<SavingsAccountDto> Accounts { get; set; } = new();

        public decimal InterestRatePercent => 6.25m;
    }

    public class ConfirmCashAdvanceViewModel
    {
        public int CreditCardId { get; set; }
        public string CardMaskedNumber { get; set; } = string.Empty;
        public string TargetAccountNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal InterestAmount { get; set; }
        public decimal TotalCharged { get; set; }

        public string AmountDisplay => Money.Format(Amount);
        public string InterestDisplay => Money.Format(InterestAmount);
        public string TotalChargedDisplay => Money.Format(TotalCharged);
    }
}
