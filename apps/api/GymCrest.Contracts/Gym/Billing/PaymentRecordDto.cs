namespace GymCrest.Contracts.Gym.Billing
{
    public class PaymentRecordDto
    {
        public Guid Id { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public DateTime PaidAt { get; set; }
    }
}
