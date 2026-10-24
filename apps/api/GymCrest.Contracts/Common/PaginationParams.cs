namespace GymCrest.Contracts.Common
{
    public class PaginationParams
    {
        private const int MaxPageSize = 100;
        public int PageNumber { get; set; } = 1;

        private int _pageSize = 10;
        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = (value > MaxPageSize) ? MaxPageSize : value;
        }

        public string? SearchTerm { get; set; }
        public bool? BypassPagination { get; set; }
        public string? StockStatus { get; set; }  // "LowStock" | "InStock"
    }
}
