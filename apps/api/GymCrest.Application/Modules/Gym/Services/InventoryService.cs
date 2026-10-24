using AutoMapper;
using GymCrest.Contracts.Gym.Inventory;
using GymCrest.Application.Modules.Gym.Interfaces;
using GymCrest.Contracts.Common;
using GymCrest.Domain.Entities;
using GymCrest.Domain.Interface;
using GymCrest.Shared.Models;

namespace GymCrest.Application.Modules.Gym.Services
{
    public class InventoryService : IInventoryService
    {
        private readonly IInventoryRepository _inventoryRepository;
        private readonly IEquipmentRepository _equipmentRepository;
        private readonly IMaintenanceRepository _maintenanceRepository;
        private readonly IEmailService _emailService;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;

        public InventoryService(
            IInventoryRepository inventoryRepository, 
            IEquipmentRepository equipmentRepository,
            IMaintenanceRepository maintenanceRepository,
            IEmailService emailService,
            IMapper mapper, 
            IUnitOfWork unitOfWork)
        {
            _inventoryRepository = inventoryRepository;
            _equipmentRepository = equipmentRepository;
            _maintenanceRepository = maintenanceRepository;
            _emailService = emailService;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> SendSaleReceiptAsync(Guid saleId)
        {
            SaleTransaction? sale = await _inventoryRepository.GetSaleByIdAsync(saleId);
            if (sale == null || sale.Member == null) return false;

            try
            {
                await _emailService.SendReceiptEmailAsync(
                    sale.Member.Email,
                    $"{sale.Member.FirstName} {sale.Member.LastName}",
                    sale.Id.ToString().Substring(0, 8).ToUpper(),
                    sale.TotalAmount.ToString("N2"),
                    sale.InventoryItem?.Name ?? "Gym Product",
                    sale.TransactionDate.ToString("MMM dd, yyyy")
                );
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<PagedResponse<InventoryItemDto>> GetProductsAsync(Guid gymId, PaginationParams pagination, Guid? branchId = null)
        {
            (IEnumerable<InventoryItem> items, int totalCount) = await _inventoryRepository.GetPagedProductsAsync(
                gymId,
                pagination.PageNumber,
                pagination.PageSize,
                pagination.SearchTerm,
                branchId,
                pagination.StockStatus);

            List<InventoryItemDto> dtos = _mapper.Map<List<InventoryItemDto>>(items);

            return new PagedResponse<InventoryItemDto>(dtos, totalCount, pagination.PageNumber, pagination.PageSize);
        }

        public async Task<InventoryItemDto> AddProductAsync(CreateProductDto dto, Guid gymId)
        {
            InventoryItem item = _mapper.Map<InventoryItem>(dto);
            item.GymId = gymId;
            item.BranchId = dto.BranchId;
            item.CreatedOn = DateTime.UtcNow;

            await _inventoryRepository.AddProductAsync(item);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<InventoryItemDto>(item);
        }

        public async Task<InventoryItemDto?> UpdateProductAsync(Guid id, CreateProductDto dto)
        {
            InventoryItem? item = await _inventoryRepository.GetProductByIdAsync(id);
            if (item == null) return null;

            _mapper.Map(dto, item);
            item.BranchId = dto.BranchId;
            item.ModifiedOn = DateTime.UtcNow;

            _inventoryRepository.UpdateProduct(item);
            await _unitOfWork.SaveChangesAsync();
            
            return _mapper.Map<InventoryItemDto>(item);
        }

        public async Task<bool> DeleteProductAsync(Guid id)
        {
            await _inventoryRepository.DeleteProductAsync(id);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RecordSaleAsync(RecordSaleDto dto, Guid gymId)
        {
            InventoryItem? product = await _inventoryRepository.GetProductByIdAsync(dto.ProductId);
            if (product == null || product.StockQuantity < dto.Quantity) return false;

            SaleTransaction transaction = new()
            {
                Id = Guid.NewGuid(),
                MemberId = dto.MemberId,
                InventoryItemId = dto.ProductId,
                Quantity = dto.Quantity,
                UnitPrice = product.SellingPrice,
                TotalAmount = product.SellingPrice * dto.Quantity,
                PaymentMethod = dto.PaymentMethod,
                TransactionDate = DateTime.UtcNow,
                GymId = gymId,
                CreatedOn = DateTime.UtcNow
            };

            bool success = await _inventoryRepository.RecordSaleAsync(transaction);
            if (success)
            {
                await _unitOfWork.SaveChangesAsync();
            }

            return success;
        }

        public async Task<PagedResponse<SaleTransactionDto>> GetSalesHistoryAsync(Guid gymId, PaginationParams pagination, Guid? branchId = null)
        {
            (IEnumerable<SaleTransaction>? items, int totalCount) = await _inventoryRepository.GetPagedSalesAsync(
                gymId,
                pagination.PageNumber,
                pagination.PageSize,
                pagination.SearchTerm,
                branchId);

            List<SaleTransactionDto> dtos = _mapper.Map<List<SaleTransactionDto>>(items);
            return new PagedResponse<SaleTransactionDto>(dtos, totalCount, pagination.PageNumber, pagination.PageSize);
        }

        public async Task<InventoryStatsDto> GetInventoryStatsAsync(Guid gymId, Guid? branchId = null)
        {
            List<InventoryItem> products = await _inventoryRepository.GetProductsByGymIdAsync(gymId, branchId);
            List<Equipment> equipment = await _equipmentRepository.GetEquipmentByGymIdAsync(gymId, branchId);
            List<SaleTransaction> sales = await _inventoryRepository.GetSalesByGymIdAsync(gymId, branchId);
            List<MaintenanceLog> logs = await _maintenanceRepository.GetAllMaintenanceLogsAsync(gymId, branchId);
            DateTime today = DateTime.UtcNow.Date;

            List<SaleTransaction> todaySales = sales.Where(s => s.TransactionDate.Date == today).ToList();
            List<SaleTransaction> recentSales = sales.Take(5).ToList();

            List<CategoryStatDto> categoryStats = products
                .GroupBy(p => p.Category)
                .Select(g => new CategoryStatDto
                {
                    Category = g.Key,
                    Count = g.Count(),
                    Value = g.Sum(p => p.SellingPrice * p.StockQuantity)
                })
                .ToList();

            List<TopProductDto> topProducts = sales
                .GroupBy(s => s.InventoryItemId)
                .Select(g => {
                    InventoryItem? product = products.FirstOrDefault(p => p.Id == g.Key);
                    return new TopProductDto
                    {
                        Name = product?.Name ?? "Unknown Product",
                        TotalSold = g.Sum(s => s.Quantity),
                        TotalRevenue = g.Sum(s => s.TotalAmount)
                    };
                })
                .OrderByDescending(x => x.TotalSold)
                .Take(5)
                .ToList();

            List<UpcomingMaintenanceDto> upcomingMaintenance = logs
                .Where(l => l.Status != "Completed" && l.EstimatedEndDate.HasValue && l.EstimatedEndDate.Value >= today)
                .OrderBy(l => l.EstimatedEndDate)
                .Take(5)
                .Select(l => new UpcomingMaintenanceDto
                {
                    EquipmentName = l.Equipment?.Name ?? "Unknown Asset",
                    ScheduledDate = l.EstimatedEndDate.Value,
                    HealthPercentage = l.Equipment?.HealthPercentage ?? 0
                })
                .ToList();

            return new InventoryStatsDto
            {
                TotalProducts = products.Count,
                LowStockCount = products.Count(p => p.StockQuantity <= p.ReorderLevel),
                MaintenanceDueCount = equipment.Count(e => e.HealthPercentage < 70 || e.IsInMaintenance),
                TodaySalesAmount = todaySales.Sum(s => s.TotalAmount),
                TodaySalesCount = todaySales.Count,
                TotalSalesAmount = sales.Sum(s => s.TotalAmount),
                TotalSalesCount = sales.Count,
                CategoryBreakdown = categoryStats,
                RecentSales = _mapper.Map<List<SaleTransactionDto>>(recentSales),
                TopProducts = topProducts,
                UpcomingMaintenance = upcomingMaintenance
            };
        }
    }
}
