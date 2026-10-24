namespace GymCrest.Domain.Entities
{
    public class SaaSPaymentTransaction : BaseEntity
    {
        public Guid GymId { get; set; }

        public Guid SubscriptionId { get; set; }

        /// <summary>Plan being purchased; applied to the subscription once the payment is confirmed.</summary>
        public Guid? PlanId { get; set; }

        public decimal Amount { get; set; }

        public string Currency { get; set; } = "INR";

        public string Status { get; set; } = "Pending";

        public string GatewayTransactionId { get; set; } = string.Empty;

        public string? GatewayResponse { get; set; }
        public string? FailureReason { get; set; }

        public Gym Gym { get; set; } = null!;
        public SubscriptionRecord Subscription { get; set; } = null!;
    }
}
