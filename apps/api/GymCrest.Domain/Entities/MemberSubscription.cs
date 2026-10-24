using GymCrest.Shared.Enums;

namespace GymCrest.Domain.Entities
{
    public class MemberSubscription: BaseEntity
    {
        public Guid MemberId { get; set; }
        public Guid GymPlanId { get; set; } 

        
        public string PlanNameSnapshot { get; set; } = string.Empty;
        public decimal PricePaid { get; set; }
        public decimal AmountPaid { get; set; }
        public int DurationMonths { get; set; }
        public int ExtendedMonths { get; set; } 
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public bool IsActive { get; set; }
        public bool IsComplementary { get; set; }
        public PaymentStatus PaymentStatus { get; set; }
                                                         
        public GymMember Member { get; set; } = null!;
        public GymPlan GymPlan { get; set; } = null!;
    }
}
