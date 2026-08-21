namespace ArtemisBank.Core.Application.Dtos.SavingsAccounts
{
    public class SavingsAccountFilterDto
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;

        public string? Identification { get; set; }

        public string? Status { get; set; }

        public string? Type { get; set; }
    }
}
