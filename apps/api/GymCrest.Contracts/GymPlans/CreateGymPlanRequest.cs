using System.ComponentModel.DataAnnotations;
namespace GymCrest.Contracts.GymPlans
{
    public class CreateGymPlanRequest
    {
        public Guid GymOwnerId { get; set; }

        public string Name { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Description { get; set; }

        public decimal Price { get; set; }

        public int DurationMonths { get; set; }

        public int? MaxBranches { get; set; }

        public string[]? Features { get; set; }

        public bool IsActive { get; set; } = true;
        
        public bool IsOffer { get; set; } = false;

        public decimal? DiscountedPrice { get; set; }
        
        public int? ExtendedMonths { get; set; }
    }
}
