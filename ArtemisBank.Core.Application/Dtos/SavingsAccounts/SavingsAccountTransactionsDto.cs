using ArtemisBank.Core.Application.Common.Models;
using ArtemisBank.Core.Application.Dtos.Transactions;

namespace ArtemisBank.Core.Application.Dtos.SavingsAccounts
{
    public class SavingsAccountTransactionsDto
    {
        public string AccountNumber { get; set; } = string.Empty;
        public string ClientFullName { get; set; } = string.Empty;
        public decimal Balance { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public PagedResult<TransactionDto> Transactions { get; set; } = PagedResult<TransactionDto>.Empty();
    }
}
