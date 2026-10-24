using System;
using System.Collections.Generic;

namespace GymCrest.Contracts.Gym.Inventory
{
    public class InventoryItemDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal SellingPrice { get; set; }
        public decimal BuyingPrice { get; set; }
        public int StockQuantity { get; set; }
        public int ReorderLevel { get; set; }
        public string? ImageUrl { get; set; }
        public string? Description { get; set; }
        public Guid? BranchId { get; set; }

        public string StockStatus => StockQuantity switch
        {
            0 => "Out of Stock",
            _ when StockQuantity <= ReorderLevel => "Low Stock",
            _ => "In Stock"
        };
    }

    public class CreateProductDto
    {
        public string Name { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal BuyingPrice { get; set; }
        public decimal SellingPrice { get; set; }
        public int StockQuantity { get; set; }
        public int ReorderLevel { get; set; }
        public string? ImageUrl { get; set; }
        public string? Description { get; set; }
        public Guid? BranchId { get; set; }
    }

    public class EquipmentDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string SerialNumber { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Condition { get; set; } = string.Empty;
        public int Health { get; set; }
        public DateTime PurchaseDate { get; set; }
        public DateTime? WarrantyExpiry { get; set; }
        public DateTime? LastService { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsInMaintenance { get; set; }
        public Guid? BranchId { get; set; }

        public string Status => IsInMaintenance ? "In Maintenance" : Health switch
        {
            < 30 => "Critical",
            < 70 => "Maintenance Due",
            _ => "Excellent"
        };
    }

    public class CreateEquipmentDto
    {
        public string Name { get; set; } = string.Empty;
        public string SerialNumber { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public DateTime PurchaseDate { get; set; }
        public DateTime? WarrantyExpiry { get; set; }
        public string Condition { get; set; } = "Excellent";
        public int MaintenanceInterval { get; set; } = 6;
        public int InitialHealth { get; set; } = 100;
        public DateTime? LastServiceDate { get; set; }
        public string? ImageUrl { get; set; }
        public Guid? BranchId { get; set; }
    }

    public class RecordSaleDto
    {
        public Guid MemberId { get; set; }
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
        public string PaymentMethod { get; set; } = "Card";
    }

    public class SaleTransactionDto
    {
        public Guid Id { get; set; }
        public string MemberName { get; set; } = string.Empty;
        public string MemberId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime Date { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
    }

    public class LogMaintenanceDto
    {
        public Guid? Id { get; set; }
        public Guid EquipmentId { get; set; }
        public string ServiceType { get; set; } = "Routine";
        public string Description { get; set; } = string.Empty;
        public string TechnicianName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime? EstimatedEndDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public decimal Cost { get; set; }
        public string Status { get; set; } = "In Progress";
        public string? Notes { get; set; }
    }

    public class MaintenanceLogDto
    {
        public Guid Id { get; set; }
        public Guid EquipmentId { get; set; }
        public string EquipmentName { get; set; } = string.Empty;
        public string ServiceType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string TechnicianName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime? EstimatedEndDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public decimal Cost { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }

    public class CategoryStatDto
    {
        public string Category { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal Value { get; set; }
    }

    public class TopProductDto
    {
        public string Name { get; set; } = string.Empty;
        public int TotalSold { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class UpcomingMaintenanceDto
    {
        public string EquipmentName { get; set; } = string.Empty;
        public DateTime? ScheduledDate { get; set; }
        public int HealthPercentage { get; set; }
    }

    public class InventoryStatsDto
    {
        public int TotalProducts { get; set; }
        public int LowStockCount { get; set; }
        public int MaintenanceDueCount { get; set; }
        public decimal TodaySalesAmount { get; set; }
        public int TodaySalesCount { get; set; }
        public decimal TotalSalesAmount { get; set; }
        public int TotalSalesCount { get; set; }
        public List<CategoryStatDto> CategoryBreakdown { get; set; } = new();
        public List<SaleTransactionDto> RecentSales { get; set; } = new();
        public List<TopProductDto> TopProducts { get; set; } = new();
        public List<UpcomingMaintenanceDto> UpcomingMaintenance { get; set; } = new();
    }
}
