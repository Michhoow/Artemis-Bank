namespace ArtemisBank.Core.Application.Dtos.SavingsAccounts
{
    /// <summary>Query params de GET /api/savings-account.</summary>
    public class SavingsAccountFilterDto
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        /// <summary>Cedula del cliente.</summary>
        public string? Identification { get; set; }
        /// <summary>activa | cancelada | todas.</summary>
        public string? Status { get; set; }
        /// <summary>principal | secundaria | todas.</summary>
        public string? Type { get; set; }
    }
}
