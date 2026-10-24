using GymCrest.Shared.Enums;

namespace GymCrest.Contracts.Members
{
    public record RenewSubscriptionRequest(
        Guid GymPlanId,
        DateTime? StartDate = null,
        PaymentStatus? PaymentStatus = null
    );
}
