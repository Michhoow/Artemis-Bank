namespace ArtemisBank.Core.Application.ViewModels
{
    /// <summary>Modelo del componente de paginacion compartido por todos los listados.</summary>
    public class PagerViewModel
    {
        public int Page { get; set; } = 1;
        public int TotalPages { get; set; }
        public string Action { get; set; } = "Index";
        public string Controller { get; set; } = string.Empty;
        /// <summary>Filtros que deben conservarse al cambiar de pagina.</summary>
        public Dictionary<string, string> RouteValues { get; set; } = new();

        public static PagerViewModel For(int page, int totalPages, string action, string controller,
            Dictionary<string, string>? routeValues = null) => new PagerViewModel
            {
                Page = page,
                TotalPages = totalPages,
                Action = action,
                Controller = controller,
                RouteValues = routeValues ?? new Dictionary<string, string>()
            };

        public Dictionary<string, string> RouteValuesFor(int targetPage)
        {
            var values = new Dictionary<string, string>(RouteValues);
            values["page"] = targetPage.ToString();
            return values;
        }
    }
}
