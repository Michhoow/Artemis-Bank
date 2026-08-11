namespace ArtemisBank.Core.Application.Common.Models
{
    /// <summary>Contenedor de paginacion usado por WebApp y WebApi. Maximo 20 registros por pagina.</summary>
    public class PagedResult<T>
    {
        public const int MaxPageSize = 20;

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = MaxPageSize;
        public int TotalRecords { get; set; }
        public int TotalPages { get; set; }
        public List<T> Data { get; set; } = new();

        public bool HasPrevious => Page > 1;
        public bool HasNext => Page < TotalPages;

        public static PagedResult<T> Create(List<T> items, int page, int pageSize, int totalRecords)
        {
            if (pageSize <= 0) pageSize = MaxPageSize;
            if (pageSize > MaxPageSize) pageSize = MaxPageSize;
            if (page <= 0) page = 1;

            return new PagedResult<T>
            {
                Page = page,
                PageSize = pageSize,
                TotalRecords = totalRecords,
                TotalPages = totalRecords == 0 ? 0 : (int)Math.Ceiling(totalRecords / (double)pageSize),
                Data = items
            };
        }

        public static PagedResult<T> Empty(int page = 1, int pageSize = MaxPageSize)
            => Create(new List<T>(), page, pageSize, 0);
    }
}
