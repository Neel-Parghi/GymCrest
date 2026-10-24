using System;
using System.Collections.Generic;

namespace GymCrest.Domain.Entities
{
    public class DietLog : BaseEntity
    {
        public Guid MemberId { get; set; }
        public User? Member { get; set; }

        public DateTime LogDate { get; set; }

        // Targets (snapshotted from the user's active diet plan for the day)
        public int TargetCalories { get; set; }
        public decimal TargetProtein { get; set; }
        public decimal TargetCarbs { get; set; }
        public decimal TargetFats { get; set; }

        // Actuals (Aggregated from MealLogEntries)
        public int TotalCalories { get; set; }
        public decimal TotalProtein { get; set; }
        public decimal TotalCarbs { get; set; }
        public decimal TotalFats { get; set; }

        public ICollection<MealLogEntry> MealEntries { get; set; } = new List<MealLogEntry>();
    }
}
