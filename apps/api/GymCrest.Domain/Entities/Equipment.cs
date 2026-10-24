using System.ComponentModel.DataAnnotations.Schema;
using GymCrest.Domain.Interface;

namespace GymCrest.Domain.Entities
{
    public class Equipment : BaseEntity, IBranchScoped
    {
        public string Name { get; set; } = string.Empty;
        public string SerialNumber { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty; // Cardio, Strength, etc.
        public DateTime PurchaseDate { get; set; }
        public DateTime? WarrantyExpiry { get; set; }
        public string CurrentCondition { get; set; } = "Excellent"; // Excellent, Good, Fair, Poor
        public int HealthPercentage { get; set; } = 100;
        public int MaintenanceIntervalMonths { get; set; } = 6;
        public DateTime? LastServiceDate { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsInMaintenance { get; set; }

        public Guid GymId { get; set; }

        [ForeignKey("GymId")]
        public virtual Gym Gym { get; set; } = null!;

        public Guid? BranchId { get; set; }

        [ForeignKey("BranchId")]
        public virtual Branch? Branch { get; set; }

        public virtual ICollection<MaintenanceLog> MaintenanceLogs { get; set; } = new List<MaintenanceLog>();
    }
}
