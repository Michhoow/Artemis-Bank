using ArtemisBank.Core.Application.Common.Models;

namespace ArtemisBank.Core.Application.Dtos.HermesPay
{
    public class HermesPayRequestDto
    {
        public string CardNumber { get; set; } = string.Empty;

        public string MonthExpirationCard { get; set; } = string.Empty;

        public string YearExpirationCard { get; set; } = string.Empty;

        public string Cvc { get; set; } = string.Empty;

        public decimal TransactionAmount { get; set; }
    }

    public class HermesPayResultDto
    {
        public bool Approved { get; set; }
        public string Message { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;
        public decimal Amount { get; set; }

        public string CardLastFourDigits { get; set; } = string.Empty;
        public string CommerceName { get; set; } = string.Empty;
        public string CommerceAccountNumber { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
        public DateTime ProcessedAt { get; set; }

        public string AmountDisplay => Money.Format(Amount);
    }

    public class CommerceTransactionDto
    {
        public string Id { get; set; } = string.Empty;
        public DateTime TransactionDate { get; set; }
        public decimal Amount { get; set; }

        public string CardLastFourDigits { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;
    }

    public class CommerceTransactionsDto
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalRecords { get; set; }
        public int TotalPages { get; set; }
        public int CommerceId { get; set; }
        public string CommerceName { get; set; } = string.Empty;
        public List<CommerceTransactionDto> Data { get; set; } = new();
    }
}
