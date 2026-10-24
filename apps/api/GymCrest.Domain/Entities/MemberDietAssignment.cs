namespace GymCrest.Domain.Entities
{
    public class MemberDietAssignment : BaseEntity
    {
        public Guid? MemberId { get; set; }
        public Guid? UserId { get; set; }
        public Guid DietPlanId { get; set; }
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;

        public GymMember? Member { get; set; }
        public User? User { get; set; }
        public DietPlan DietPlan { get; set; } = null!;
    }
}
