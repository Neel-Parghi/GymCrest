using System;

namespace GymCrest.Contracts.Payments
{
    public class InitiateMemberPaymentRequest
    {
        public Guid UserId { get; set; }
        public Guid PlanId { get; set; }
    }

    public class VerifyMemberPaymentRequest
    {
        public string RazorpayPaymentId { get; set; } = string.Empty;
        public string RazorpayOrderId { get; set; } = string.Empty;
        public string RazorpaySignature { get; set; } = string.Empty;
        public Guid PlanId { get; set; }
        public Guid UserId { get; set; }
    }

    public class PaymentInitiationResponse
    {
        public string OrderId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string KeyId { get; set; } = string.Empty;
    }
    
    public class PaymentVerificationResponse
    {
        public bool IsSuccess { get; set; }
        public Guid SubscriptionId { get; set; }
    }

    public class OfflineCheckoutRequest
    {
        public Guid UserId { get; set; }
        public Guid PlanId { get; set; }
    }

    public class OfflineCheckoutResponse
    {
        public bool IsSuccess { get; set; }
        public Guid SubscriptionId { get; set; }
    }
}
