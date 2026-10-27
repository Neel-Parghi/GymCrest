using System.ComponentModel.DataAnnotations;
namespace GymCrest.Contracts.GymPlans
{
    public class GymPlanDto
    {
        public Guid Id { get; set; }

        public Guid GymOwnerId { get; set; }

        public string Name { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Description { get; set; }

        public decimal Price { get; set; }

        public int DurationMonths { get; set; }

        public int? MaxBranches { get; set; }

        public string[]? Features { get; set; }

        public bool IsActive { get; set; }
        
        public bool IsOffer { get; set; }
        
        public decimal? DiscountedPrice { get; set; }
        
        public int? ExtendedMonths { get; set; }

        public DateTime CreatedOn { get; set; }

        public Guid CreatedBy { get; set; }

        public DateTime? ModifiedOn { get; set; }

        public Guid? ModifiedBy { get; set; }
    }
}
