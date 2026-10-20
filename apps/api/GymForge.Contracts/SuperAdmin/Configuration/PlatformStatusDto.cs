namespace GymForge.Contracts.SuperAdmin.Configuration
{
    /// <summary>The public part of the platform settings: safe to show anyone, including signed-out visitors.</summary>
    public class PlatformStatusDto
    {
        public string? SupportEmail { get; set; }
        public string? SupportPhone { get; set; }
        public int GracePeriodDays { get; set; }
        public bool FoodSearchEnabled { get; set; } = true;

        /// <summary>True while maintenance mode is on and the current time is inside its window (if one is set).</summary>
        public bool MaintenanceActive { get; set; }
        public DateTime? MaintenanceStartTime { get; set; }
        public DateTime? MaintenanceEndTime { get; set; }
    }
}
