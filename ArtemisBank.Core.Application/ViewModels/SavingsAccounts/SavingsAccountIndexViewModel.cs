using ArtemisBank.Core.Application.Common.Models;

namespace ArtemisBank.Core.Application.ViewModels.SavingsAccounts
{
    public class SavingsAccountIndexViewModel
    {
        public PagedResult<SavingsAccountViewModel> Accounts { get; set; }
            = PagedResult<SavingsAccountViewModel>.Empty();

        public string? Identification { get; set; }

        public string Status { get; set; } = "activa";

        public string Type { get; set; } = "todas";

        public string? InfoMessage { get; set; }
    }
}
