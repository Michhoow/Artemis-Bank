namespace ArtemisBank.Core.Application.ViewModels.Home
{
    /// <summary>Indicadores del dia del cajero autenticado. Nunca incluye operaciones de otros cajeros.</summary>
    public class CashierHomeViewModel
    {
        public int TodayTransactions { get; set; }
        public int TodayPayments { get; set; }
        public int TodayDeposits { get; set; }
        public int TodayWithdrawals { get; set; }
    }
}
