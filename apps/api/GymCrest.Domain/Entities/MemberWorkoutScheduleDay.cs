namespace GymCrest.Domain.Entities
{
    public class MemberWorkoutScheduleDay : BaseEntity
    {
        public Guid MemberPlanAssignmentId { get; set; }
        public string DayOfWeek { get; set; } = string.Empty; // e.g. "Monday", "Tuesday"
        public Guid? WorkoutPlanDayId { get; set; } // Null if rest day
        public bool IsRestDay { get; set; } = false;

        public MemberPlanAssignment MemberPlanAssignment { get; set; } = null!;
        public WorkoutPlanDay? WorkoutPlanDay { get; set; }
    }
}
