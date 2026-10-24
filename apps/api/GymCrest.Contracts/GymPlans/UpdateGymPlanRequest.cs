namespace GymCrest.Contracts.GymPlans
{
    public class UpdateGymPlanRequest
    {
        public Guid Id { get; set; }

        public Guid GymOwnerId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public decimal Price { get; set; }

        public int DurationMonths { get; set; }

        public int? MaxBranches { get; set; }

        public string[]? Features { get; set; }

        public bool IsActive { get; set; }
    
        public bool IsOffer { get; set; }
    
        public decimal? DiscountedPrice { get; set; }
    
        public int? ExtendedMonths { get; set; }
    }
}
