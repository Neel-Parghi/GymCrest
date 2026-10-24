namespace GymCrest.Domain.Entities
{
    public class WorkoutSessionLog : BaseEntity
    {
        public Guid? MemberId { get; set; }
        public Guid? UserId { get; set; }
        public DateTime Date { get; set; }
        public string DayName { get; set; } = string.Empty;
        public int ExercisesCompleted { get; set; }
        public int TotalSets { get; set; }
        public string Status { get; set; } = "Completed"; // Completed, Planned, RestDay
        public string? Notes { get; set; }
        
        public GymMember? Member { get; set; }
        public User? User { get; set; }
        public ICollection<LoggedExercise> LoggedExercises { get; set; } = [];
    }
}
