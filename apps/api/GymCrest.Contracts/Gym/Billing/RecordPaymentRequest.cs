using System.ComponentModel.DataAnnotations;
namespace GymCrest.Contracts.Gym.Billing
{
    public class RecordPaymentRequest
    {
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = "UPI";
        [MaxLength(2000)]
        public string? Notes { get; set; }
    }
}
