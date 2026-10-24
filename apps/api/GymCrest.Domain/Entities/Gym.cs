using System.ComponentModel.DataAnnotations.Schema;

namespace GymCrest.Domain.Entities
{
    public class Gym : BaseEntity
    {
        public string GymName { get; set; } = string.Empty;

        public string? BrandName { get; set; }

        public string? Email { get; set; } = string.Empty;
        
        public string? Phone { get; set; } = string.Empty;

        public string? WebsiteUrl { get; set; }

        public string? GstNumber { get; set; }

        public string? RegistrationNumber { get; set; }

        public DateTime? EstablishedDate { get; set; }

        public Guid OwnerUserId { get; set; }

        public Guid? AddressId { get; set; }

        public string? LogoUrl { get; set; } = string.Empty;

        public string? BannerUrl { get; set; } = string.Empty;

        public string? Description {  get; set; } = string.Empty;

        public bool IsActive { get; set; }

        public bool IsVerified { get; set; }

        // Billing & Invoicing Configuration Settings
        public string? InvoicePrefix { get; set; } = "GF-";

        [Column(TypeName = "numeric(5,2)")]
        public decimal DefaultTaxRate { get; set; } = 18.0m;

        public int OverdueGraceDays { get; set; } = 7;

        public bool AutoEmailReceipts { get; set; } = true;

        public string? RoleRightsMatrixJson { get; set; }

        [ForeignKey(nameof(OwnerUserId))]
        public User Owner { get; set; } = null!;

        public Address? Address { get; set; }

        public GymConfiguration? Configuration { get; set; }

        public ICollection<Branch>? Branches { get; set; }
    }
}
