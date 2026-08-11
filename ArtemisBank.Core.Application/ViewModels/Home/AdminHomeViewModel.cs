namespace ArtemisBank.Core.Application.ViewModels.Home
{
    /// <summary>Indicadores generales del sistema mostrados en el Home del administrador.</summary>
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
        /// <summary>Deuda total de clientes activos / cantidad de clientes activos. 0.00 si no hay clientes activos.</summary>
        public decimal AverageDebtPerClient { get; set; }
    }
}
