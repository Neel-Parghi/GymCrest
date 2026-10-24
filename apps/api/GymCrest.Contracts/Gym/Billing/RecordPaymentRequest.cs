namespace GymCrest.Contracts.Gym.Billing
{
    public class RecordPaymentRequest
    {
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = "UPI";
        public string? Notes { get; set; }
    }
}
