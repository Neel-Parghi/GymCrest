namespace GymCrest.Contracts.SuperAdmin.Configuration
{
    public class SaaSConfigurationDto
    {
        public Guid Id { get; set; }
        public string PlatformName { get; set; } = string.Empty;
        public string BillingEmail { get; set; } = string.Empty;
        public decimal TaxPercentage { get; set; }
        public int GracePeriodDays { get; set; }
        public string Currency { get; set; } = "INR";
        public string? BillingAddress { get; set; }
        public string? GstNo { get; set; }
        public string? SupportPhone { get; set; }
        public string? SupportEmail { get; set; }
        public bool IsMaintenanceMode { get; set; }
        public bool IsFoodSearchEnabled { get; set; } = true;
        public string? TermsUrl { get; set; }
        public string? PrivacyUrl { get; set; }
        public DateTime? MaintenanceStartTime { get; set; }
        public DateTime? MaintenanceEndTime { get; set; }
        public decimal YearlyRevenueTarget { get; set; }
        public int SubscriptionTarget { get; set; }
        public decimal UptimeThreshold { get; set; }
    }
}
