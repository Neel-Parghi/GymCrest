namespace GymCrest.Domain.Entities
{
    public class SubscriptionRecord : BaseEntity
    {
        public Guid GymId { get; set; }

        public Guid PlanId { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public bool IsActive { get; set; }

        public bool IsTrial { get; set; }

        public decimal PriceAtPurchase { get; set; }

        public string? Notes { get; set; }

        // Navigation
        public Gym Gym { get; set; } = null!;

        public Plan Plan { get; set; } = null!;
    }
}
