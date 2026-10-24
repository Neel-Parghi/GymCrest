namespace GymCrest.Contracts.Gym.Billing
{
    public class CreateCustomInvoiceRequest
    {
        public Guid MemberId { get; set; }
        public Guid? TrainerId { get; set; }
        public string BillingType { get; set; } = "Day Pass";
        public decimal Amount { get; set; }
        public string Status { get; set; } = "Paid"; 
        public string PaymentMethod { get; set; } = "UPI";
    }
}
