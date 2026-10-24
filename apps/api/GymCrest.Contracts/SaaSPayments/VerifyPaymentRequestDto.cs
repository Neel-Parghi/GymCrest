namespace GymCrest.Contracts.SaaSPayments
{
    public class VerifyPaymentRequestDto
    {
        public string OrderId { get; set; } = string.Empty;
        public string PaymentId { get; set; } = string.Empty;
        public string Signature { get; set; } = string.Empty;
    }
}
