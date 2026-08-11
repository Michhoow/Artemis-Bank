using ArtemisBank.Core.Application.ViewModels.SavingsAccounts;

namespace ArtemisBank.Core.Application.ViewModels.Client
{
    public class ClientLoanViewModel
    {
        public int Id { get; set; }
        public string LoanNumber { get; set; } = string.Empty;
        public decimal ApprovedCapital { get; set; }
        public int TotalInstallments { get; set; }
        public int PaidInstallments { get; set; }
        public decimal PendingAmount { get; set; }
        public decimal AnnualInterestRate { get; set; }
        public int TermInMonths { get; set; }
        /// <summary>Al día o En mora.</summary>
        public string Status { get; set; } = "Al día";
    }

    public class ClientCreditCardViewModel
    {
        public int Id { get; set; }
        /// <summary>Siempre enmascarado. El numero completo nunca se muestra.</summary>
        public string MaskedNumber { get; set; } = string.Empty;
        public string LastFourDigits { get; set; } = string.Empty;
        public decimal CreditLimit { get; set; }
        public string ExpirationDate { get; set; } = string.Empty;
        public decimal Debt { get; set; }
    }

    /// <summary>Home del cliente: productos financieros activos.</summary>
    public class ClientHomeViewModel
    {
        public List<SavingsAccountViewModel> SavingsAccounts { get; set; } = new();
        public List<ClientLoanViewModel> Loans { get; set; } = new();
        public List<ClientCreditCardViewModel> CreditCards { get; set; } = new();

        public bool HasSavingsAccounts => SavingsAccounts.Count > 0;
        public bool HasLoans => Loans.Count > 0;
        public bool HasCreditCards => CreditCards.Count > 0;
        public bool HasAnyProduct => HasSavingsAccounts || HasLoans || HasCreditCards;
    }
}
