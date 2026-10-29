using System.ComponentModel.DataAnnotations;
namespace GymCrest.Contracts.Gym.Management
{
    public class UpdateGymDto
    {
        public Guid Id { get; set; }
        public string GymName { get; set; } = string.Empty;
        public string BrandName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? WebsiteUrl { get; set; }
        [MaxLength(2000)]
        public string? Description { get; set; }
        public string? GstNumber { get; set; }
        public string? RegistrationNumber { get; set; }
        public DateTime? EstablishedDate { get; set; }
        [MaxLength(10)]
        public string? InvoicePrefix { get; set; }
        [Range(0, 50)]
        public decimal? DefaultTaxRate { get; set; }
        [Range(0, 365)]
        public int? OverdueGraceDays { get; set; }
        public bool? AutoEmailReceipts { get; set; }
        public bool IsActive { get; set; }
        public bool IsVerified { get; set; }
    }
}
