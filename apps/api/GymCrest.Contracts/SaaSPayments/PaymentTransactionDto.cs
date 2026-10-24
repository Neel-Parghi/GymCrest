namespace GymCrest.Contracts.SaaSPayments
{
    public class PaymentTransactionDto
    {
        public Guid Id { get; set; }
        public string GymName { get; set; } = string.Empty;
        public string PlanName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "INR";
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string GatewayTransactionId { get; set; } = string.Empty;
    }
}
