namespace GymCrest.Domain.Entities
{
    public class MemberPlanAssignment : BaseEntity
    {
        public Guid? MemberId { get; set; }
        public Guid? UserId { get; set; }
        public Guid WorkoutPlanId { get; set; }
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
        
        public GymMember? Member { get; set; }
        public User? User { get; set; }
        public WorkoutPlan WorkoutPlan { get; set; } = null!;

        public ICollection<MemberWorkoutScheduleDay> CustomScheduleDays { get; set; } = [];
    }
}
