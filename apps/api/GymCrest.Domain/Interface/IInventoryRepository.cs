using GymCrest.Domain.Entities;

namespace GymCrest.Domain.Interface
{
    public interface IInventoryRepository
    {
        // Inventory Items
        Task<InventoryItem?> GetProductByIdAsync(Guid id);
        Task<List<InventoryItem>> GetProductsByGymIdAsync(Guid gymId, Guid? branchId = null);
        Task<(IEnumerable<InventoryItem> items, int totalCount)> GetPagedProductsAsync(Guid gymId, int pageNumber, int pageSize, string? searchTerm, Guid? branchId = null, string? stockStatus = null);
        Task AddProductAsync(InventoryItem item);
        InventoryItem UpdateProduct(InventoryItem item);
        Task DeleteProductAsync(Guid id);

        // Sales & Stock
        Task<bool> RecordSaleAsync(SaleTransaction transaction);
        Task<List<SaleTransaction>> GetSalesByGymIdAsync(Guid gymId, Guid? branchId = null);
        Task<(IEnumerable<SaleTransaction> items, int totalCount)> GetPagedSalesAsync(Guid gymId, int pageNumber, int pageSize, string? searchTerm, Guid? branchId = null);
        Task<SaleTransaction?> GetSaleByIdAsync(Guid id);
    }
}
