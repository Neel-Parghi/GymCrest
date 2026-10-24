using System.ComponentModel.DataAnnotations.Schema;
using GymCrest.Domain.Interface;

namespace GymCrest.Domain.Entities
{
    public class InventoryItem : BaseEntity, IBranchScoped
    {
        public string Name { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty; // Supplements, Apparel, etc.
        public string? Description { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal BuyingPrice { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal SellingPrice { get; set; }
        
        public int StockQuantity { get; set; }
        public int ReorderLevel { get; set; } = 5;
        public string? ImageUrl { get; set; }
        public bool IsActive { get; set; } = true;

        public Guid GymId { get; set; }
        public Guid? BranchId { get; set; }
        
        [ForeignKey("GymId")]
        public virtual Gym Gym { get; set; } = null!;

        [ForeignKey("BranchId")]
        public virtual Branch? Branch { get; set; }
    }
}
