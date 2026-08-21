using ArtemisBank.Core.Application.Common.Models;
using ArtemisBank.Core.Application.ViewModels.Transactions;

namespace ArtemisBank.Core.Application.ViewModels.SavingsAccounts
{
    public class AccountTransactionsViewModel
    {
        public string AccountNumber { get; set; } = string.Empty;
        public string ClientFullName { get; set; } = string.Empty;
        public decimal Balance { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public PagedResult<TransactionViewModel> Transactions { get; set; }
            = PagedResult<TransactionViewModel>.Empty();
    }
}
