using System.ComponentModel.DataAnnotations;

namespace GymForge.Domain.Entities
{
    public class SaaSConfiguration : BaseEntity
    {
        [Required]
        [MaxLength(100)]
        public string PlatformName { get; set; } = "GymCrest";

        [Required]
        [EmailAddress]
        public string BillingEmail { get; set; } = string.Empty;

        public decimal TaxPercentage { get; set; } = 18.0m;

        public int GracePeriodDays { get; set; } = 7;

        public string Currency { get; set; } = "INR";

        public string? BillingAddress { get; set; }
        
        public string? GstNo { get; set; }
        
        public string? SupportPhone { get; set; }
        public string? SupportEmail { get; set; }
        public bool IsMaintenanceMode { get; set; } = false;
        public string? TermsUrl { get; set; }
        public string? PrivacyUrl { get; set; }
        public DateTime? MaintenanceStartTime { get; set; }
        public DateTime? MaintenanceEndTime { get; set; }

        // Strategic Targets
        public decimal YearlyRevenueTarget { get; set; } = 1200000.0m;
        public int SubscriptionTarget { get; set; } = 100;
        public decimal UptimeThreshold { get; set; } = 99.9m;
    }
}
