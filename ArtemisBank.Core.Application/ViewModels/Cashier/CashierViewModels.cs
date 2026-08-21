using System.ComponentModel.DataAnnotations;

namespace ArtemisBank.Core.Application.ViewModels.Cashier
{
    public class DepositViewModel
    {
        [Required(ErrorMessage = "El número de cuenta destino es requerido.")]
        [RegularExpression("^[0-9]{9}$", ErrorMessage = "El número de cuenta debe contener exactamente 9 dígitos.")]
        [Display(Name = "Número de cuenta destino")]
        public string TargetAccountNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "El monto a depositar es requerido.")]
        [Range(0.01, 999999999999999.99, ErrorMessage = "El monto a depositar debe ser mayor que cero.")]
        [Display(Name = "Monto a depositar")]
        public decimal? Amount { get; set; }
    }

    public class WithdrawalViewModel
    {
        [Required(ErrorMessage = "El número de cuenta origen es requerido.")]
        [RegularExpression("^[0-9]{9}$", ErrorMessage = "El número de cuenta debe contener exactamente 9 dígitos.")]
        [Display(Name = "Número de cuenta origen")]
        public string SourceAccountNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "El monto a retirar es requerido.")]
        [Range(0.01, 999999999999999.99, ErrorMessage = "El monto a retirar debe ser mayor que cero.")]
        [Display(Name = "Monto a retirar")]
        public decimal? Amount { get; set; }
    }

    public class CashierCardPaymentViewModel
    {
        [Required(ErrorMessage = "El número de cuenta origen es requerido.")]
        [RegularExpression("^[0-9]{9}$", ErrorMessage = "El número de cuenta debe contener exactamente 9 dígitos.")]
        [Display(Name = "Número de cuenta origen")]
        public string SourceAccountNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "El número de tarjeta de crédito es requerido.")]
        [RegularExpression("^[0-9]{16}$", ErrorMessage = "El número de tarjeta debe contener 16 dígitos.")]
        [Display(Name = "Número de tarjeta de crédito")]
        public string CardNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "El monto a pagar es requerido.")]
        [Range(0.01, 999999999999999.99, ErrorMessage = "El monto a pagar debe ser mayor que cero.")]
        [Display(Name = "Monto a pagar")]
        public decimal? Amount { get; set; }
    }

    public class CashierLoanPaymentViewModel
    {
        [Required(ErrorMessage = "El número de cuenta origen es requerido.")]
        [RegularExpression("^[0-9]{9}$", ErrorMessage = "El número de cuenta debe contener exactamente 9 dígitos.")]
        [Display(Name = "Número de cuenta origen")]
        public string SourceAccountNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "El número de préstamo es requerido.")]
        [RegularExpression("^[0-9]{9}$", ErrorMessage = "El número de préstamo debe contener 9 dígitos.")]
        [Display(Name = "Número de préstamo")]
        public string LoanNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "El monto a pagar es requerido.")]
        [Range(0.01, 999999999999999.99, ErrorMessage = "El monto a pagar debe ser mayor que cero.")]
        [Display(Name = "Monto a pagar")]
        public decimal? Amount { get; set; }
    }

    public class ThirdPartyTransactionViewModel
    {
        [Required(ErrorMessage = "El número de cuenta origen es requerido.")]
        [RegularExpression("^[0-9]{9}$", ErrorMessage = "El número de cuenta debe contener exactamente 9 dígitos.")]
        [Display(Name = "Número de cuenta origen")]
        public string SourceAccountNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "El número de cuenta destino es requerido.")]
        [RegularExpression("^[0-9]{9}$", ErrorMessage = "El número de cuenta debe contener exactamente 9 dígitos.")]
        [Display(Name = "Número de cuenta destino")]
        public string TargetAccountNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "El monto de la transacción es requerido.")]
        [Range(0.01, 999999999999999.99, ErrorMessage = "El monto de la transacción debe ser mayor que cero.")]
        [Display(Name = "Monto de la transacción")]
        public decimal? Amount { get; set; }
    }

    public class CashierConfirmViewModel
    {
        public string Operation { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string ConfirmationMessage { get; set; } = string.Empty;

        public string? SourceAccountNumber { get; set; }
        public string? SourceOwnerFullName { get; set; }
        public string? TargetAccountNumber { get; set; }
        public string? TargetOwnerFullName { get; set; }

        public string? CardNumber { get; set; }
        public string? CardLastFour { get; set; }
        public string? CardOwnerFullName { get; set; }
        public int? CreditCardId { get; set; }

        public string? LoanNumber { get; set; }
        public string? LoanOwnerFullName { get; set; }
        public int? LoanId { get; set; }

        public decimal EnteredAmount { get; set; }

        public decimal EffectiveAmount { get; set; }
        public bool ShowEffectiveAmount { get; set; }
    }
}
