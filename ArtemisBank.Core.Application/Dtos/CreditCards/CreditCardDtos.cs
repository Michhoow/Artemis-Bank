using ArtemisBank.Core.Application.Common.Models;

namespace ArtemisBank.Core.Application.Dtos.CreditCards
{
    public class CreditCardDto
    {
        public int Id { get; set; }
        public string ClientId { get; set; } = string.Empty;
        public string ClientFullName { get; set; } = string.Empty;
        public string ClientIdentification { get; set; } = string.Empty;

        public string MaskedNumber { get; set; } = string.Empty;
        public string LastFourDigits { get; set; } = string.Empty;

        public decimal CreditLimit { get; set; }
        public decimal Debt { get; set; }
        public decimal AvailableCredit { get; set; }

        public string ExpirationDate { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }

        public string CreditLimitDisplay => Money.Format(CreditLimit);
        public string DebtDisplay => Money.Format(Debt);
        public string AvailableCreditDisplay => Money.Format(AvailableCredit);
        public bool HasDebt => Debt > 0m;
    }

    public class CardConsumptionDto
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public decimal Amount { get; set; }

        public string Type { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;
        public string? CommerceName { get; set; }
        public string? RejectionReason { get; set; }

        public string AmountDisplay => Money.Format(Amount);
        public string DateDisplay => CreatedAt.ToString("dd/MM/yyyy hh:mm tt");
    }

    public class CreditCardDetailDto : CreditCardDto
    {
        public List<CardConsumptionDto> Consumptions { get; set; } = new();
    }

    public class CreditCardFilterDto
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;

        public string? Identification { get; set; }

        public string? Status { get; set; }
    }

    public class CreateCreditCardDto
    {
        public string ClientId { get; set; } = string.Empty;
        public decimal CreditLimit { get; set; }
    }

    public class UpdateCardLimitDto
    {
        public decimal CreditLimit { get; set; }
    }

    public class CreatedCreditCardDto
    {
        public CreditCardDto Card { get; set; } = new();

        public string Cvc { get; set; } = string.Empty;
    }

    public class CashAdvanceRequestDto
    {
        public int CreditCardId { get; set; }
        public string TargetAccountNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }

    public class CashAdvanceResultDto
    {
        public decimal Amount { get; set; }
        public decimal InterestAmount { get; set; }
        public decimal TotalCharged { get; set; }
        public string CardLastFourDigits { get; set; } = string.Empty;
        public string TargetAccountNumber { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
    }
}
