namespace GymCrest.Contracts.Gym.Owners
{
    public class UpdateMyGymDto
    {
        public string GymName { get; set; } = string.Empty;
        public string? BrandName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? WebsiteUrl { get; set; }
        public string? Description { get; set; }
        public string? GstNumber { get; set; }
        public string? RegistrationNumber { get; set; }
        public string? LogoUrl { get; set; }
        public string? BannerUrl { get; set; }

        // Gym Tax and Billing Configuration Settings updates
        public string? InvoicePrefix { get; set; }
        public decimal? DefaultTaxRate { get; set; }
        public int? OverdueGraceDays { get; set; }
        public bool? AutoEmailReceipts { get; set; }
    }
}
