using System.ComponentModel.DataAnnotations;
namespace GymCrest.Contracts.Gym.Management
{
    public class GymDto
    {
        public string Name { get; set; } = null!;
        public string? BrandName { get; set; }
        [MaxLength(2000)]
        public string? Description { get; set; }
        public DateTime? EstablishedDate { get; set; }

        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? WebsiteUrl { get; set; }

        public string? GstNumber { get; set; }
        public string? RegistrationNumber { get; set; }

        public string? LogoUrl { get; set; }
        public string? CoverImageUrl { get; set; }

        public Guid PlanId { get; set; }
        public bool IsTrial { get; set; }
    }
}
