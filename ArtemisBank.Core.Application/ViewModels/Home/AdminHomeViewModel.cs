namespace ArtemisBank.Core.Application.ViewModels.Home
{
    public class AdminHomeViewModel
    {
        public int TotalHistoricTransactions { get; set; }
        public int TodayTransactions { get; set; }
        public int TotalHistoricPayments { get; set; }
        public int TodayPayments { get; set; }
        public int ActiveClients { get; set; }
        public int InactiveClients { get; set; }
        public int TotalFinancialProducts { get; set; }
        public int ActiveLoans { get; set; }
        public int ActiveCreditCards { get; set; }
        public int ActiveSavingsAccounts { get; set; }

        public decimal AverageDebtPerClient { get; set; }
    }
}
