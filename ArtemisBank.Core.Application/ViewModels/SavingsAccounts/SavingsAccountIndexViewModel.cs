using ArtemisBank.Core.Application.Common.Models;

namespace ArtemisBank.Core.Application.ViewModels.SavingsAccounts
{
    /// <summary>Listado principal de gestion de cuentas de ahorro con filtros y paginacion.</summary>
    public class SavingsAccountIndexViewModel
    {
        public PagedResult<SavingsAccountViewModel> Accounts { get; set; }
            = PagedResult<SavingsAccountViewModel>.Empty();

        public string? Identification { get; set; }
        /// <summary>activa | cancelada | todas.</summary>
        public string Status { get; set; } = "activa";
        /// <summary>todas | principal | secundaria.</summary>
        public string Type { get; set; } = "todas";

        public string? InfoMessage { get; set; }
    }
}
