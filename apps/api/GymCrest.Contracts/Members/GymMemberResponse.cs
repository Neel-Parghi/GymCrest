using GymCrest.Contracts.Gym.Shared;
using GymCrest.Shared.Enums;

namespace GymCrest.Contracts.Members
{
    public class GymMemberResponse
    {
        public Guid Id { get; set; }
        public string MembershipNumber { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public Gender Gender { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public string? BloodGroup { get; set; }
        public AddressDto? Address { get; set; }
        public string? MedicalConditions { get; set; }
        public List<string>? FitnessGoals { get; set; }
        public DateTime JoiningDate { get; set; }
        public MemberStatus Status { get; set; }
        public DateTime? FreezeStartDate { get; set; }
        public DateTime? FreezeUntil { get; set; }
        public Guid? BranchId { get; set; }
        public MemberSubscriptionResponse? CurrentSubscription { get; set; }
    }

    public class MemberSubscriptionResponse
    {
        public Guid Id { get; set; }
        public Guid GymPlanId { get; set; }
        public string PlanNameSnapshot { get; set; } = string.Empty;
        public decimal PricePaid { get; set; }
        public decimal AmountPaid { get; set; }
        public int DurationMonths { get; set; }
        public int ExtendedMonths { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
        public PaymentStatus PaymentStatus { get; set; }
    }
}
