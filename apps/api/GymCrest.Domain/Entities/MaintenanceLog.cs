using System.ComponentModel.DataAnnotations.Schema;

namespace GymCrest.Domain.Entities
{
    public class MaintenanceLog : BaseEntity
    {
        public string ServiceType { get; set; } = "Routine"; // Routine, Repair, Part Replacement
        public string Description { get; set; } = string.Empty;
        public string TechnicianName { get; set; } = string.Empty;
        
        [Column("ScheduledDate")]
        public DateTime StartDate { get; set; } = DateTime.UtcNow;
        public DateTime? EstimatedEndDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public decimal Cost { get; set; }
        public string Status { get; set; } = "In Progress"; // Scheduled, In Progress, Completed
        public string? Notes { get; set; }

        public Guid EquipmentId { get; set; }

        [ForeignKey("EquipmentId")]
        public virtual Equipment Equipment { get; set; } = null!;
    }
}
