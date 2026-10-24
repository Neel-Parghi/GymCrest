namespace GymForge.Contracts.SaaSPlan
{
    public class CreateSaaSPlanDto
    {
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public decimal Price { get; set; }

        public int DurationInDays { get; set; }

        public int? MaxBranches { get; set; }
        public int? MaxMembers { get; set; }

        public string[]? Features { get; set; }

        public bool IsActive { get; set; }

        public bool IsTrial { get; set; }
    }
}
