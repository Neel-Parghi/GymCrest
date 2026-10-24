using GymCrest.Domain.Entities;
using GymCrest.Domain.Interface;
using GymCrest.Infrastructure.Extensions;
using GymCrest.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GymCrest.Infrastructure.Repositories
{
    public class InventoryRepository : IInventoryRepository
    {
        private readonly AppDbContext _context;

        public InventoryRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddProductAsync(InventoryItem item)
        {
            await _context.InventoryItems.AddAsync(item);
        }

        public async Task DeleteProductAsync(Guid id)
        {
            InventoryItem? item = await _context.InventoryItems.FindAsync(id);
            if (item != null)
            {
                _context.InventoryItems.Remove(item);
            }
        }

        public async Task<InventoryItem?> GetProductByIdAsync(Guid id)
        {
            return await _context.InventoryItems
                .AsNoTracking()
                .Include(x => x.Gym)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<List<InventoryItem>> GetProductsByGymIdAsync(Guid gymId, Guid? branchId = null)
        {
            IQueryable<InventoryItem> query = _context.InventoryItems
                .TagWith("GetProductsByGymId")
                .AsNoTracking()
                .Where(x => x.GymId == gymId);

            query = query.WhereBranchContext(_context.Branches, branchId);

            return await query.OrderByDescending(x => x.ModifiedOn ?? x.CreatedOn).ToListAsync();
        }

        public async Task<(IEnumerable<InventoryItem> items, int totalCount)> GetPagedProductsAsync(Guid gymId, int pageNumber, int pageSize, string? searchTerm, Guid? branchId = null, string? stockStatus = null)
        {
            IQueryable<InventoryItem> query = _context.InventoryItems
                .AsNoTracking()
                .Where(x => x.GymId == gymId);

            query = query.WhereBranchContext(_context.Branches, branchId);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                query = query.Where(x => x.Name.ToLower().Contains(searchTerm) || x.SKU.ToLower().Contains(searchTerm));
            }

            if (stockStatus == "LowStock")
                query = query.Where(x => x.StockQuantity <= x.ReorderLevel);
            else if (stockStatus == "InStock")
                query = query.Where(x => x.StockQuantity > x.ReorderLevel);

            int totalCount = await query.CountAsync();

            List<InventoryItem> items = await query
                .OrderByDescending(x => x.ModifiedOn ?? x.CreatedOn)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public InventoryItem UpdateProduct(InventoryItem item)
        {
            if (_context.Entry(item).State == EntityState.Detached)
                _context.InventoryItems.Update(item);
            return item;
        }

        public async Task<bool> RecordSaleAsync(SaleTransaction transaction)
        {
            InventoryItem? product = await _context.InventoryItems.FindAsync(transaction.InventoryItemId);
            if (product == null || product.StockQuantity < transaction.Quantity)
            {
                return false;
            }

            product.StockQuantity -= transaction.Quantity;
            transaction.BranchId = product.BranchId;
            
            await _context.SaleTransactions.AddAsync(transaction);
            return true;
        }

        public async Task<List<SaleTransaction>> GetSalesByGymIdAsync(Guid gymId, Guid? branchId = null)
        {
            IQueryable<SaleTransaction> query = _context.SaleTransactions
                .AsNoTracking()
                .Include(x => x.InventoryItem)
                .Include(x => x.Member)
                .Where(x => x.GymId == gymId);

            query = query.WhereBranchContext(_context.Branches, branchId);

            return await query
                .OrderByDescending(x => x.TransactionDate)
                .ToListAsync();
        }

        public async Task<(IEnumerable<SaleTransaction> items, int totalCount)> GetPagedSalesAsync(Guid gymId, int pageNumber, int pageSize, string? searchTerm, Guid? branchId = null)
        {
            IQueryable<SaleTransaction> query = _context.SaleTransactions
                .AsNoTracking()
                .Include(x => x.InventoryItem)
                .Include(x => x.Member)
                .Where(x => x.GymId == gymId);

            query = query.WhereBranchContext(_context.Branches, branchId);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                string lowerTerm = searchTerm.ToLower();
                query = query.Where(x => 
                    (x.InventoryItem != null && x.InventoryItem.Name.ToLower().Contains(lowerTerm)) || 
                    (x.Member != null && (x.Member.FirstName.ToLower().Contains(lowerTerm) || x.Member.LastName.ToLower().Contains(lowerTerm)))
                );
            }

            int totalCount = await query.CountAsync();

            List<SaleTransaction> items = await query
                .OrderByDescending(x => x.TransactionDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<SaleTransaction?> GetSaleByIdAsync(Guid id)
        {
            return await _context.SaleTransactions
                .Include(x => x.InventoryItem)
                .Include(x => x.Member)
                .FirstOrDefaultAsync(x => x.Id == id);
        }
    }
}
