using GymCrest.Shared.Enums;

namespace GymCrest.Contracts.Members
{
    public class MyGymMembershipResponse
    {
        public Guid GymId { get; set; }
        public string GymName { get; set; } = string.Empty;
        public string? GymLogoUrl { get; set; }
        public string MembershipNumber { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime JoiningDate { get; set; }
        
        public MemberSubscriptionResponse? CurrentSubscription { get; set; }
    }
}
