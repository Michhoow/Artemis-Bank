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

        public string Status { get; set; } = "Al día";
    }

    public class ClientCreditCardViewModel
    {
        public int Id { get; set; }

        public string MaskedNumber { get; set; } = string.Empty;
        public string LastFourDigits { get; set; } = string.Empty;
        public decimal CreditLimit { get; set; }
        public string ExpirationDate { get; set; } = string.Empty;
        public decimal Debt { get; set; }
    }

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
