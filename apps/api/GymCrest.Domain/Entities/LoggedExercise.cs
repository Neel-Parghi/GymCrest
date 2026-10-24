namespace GymCrest.Domain.Entities
{
    public class LoggedExercise : BaseEntity
    {
        public Guid WorkoutSessionLogId { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool Skipped { get; set; } = false;
        public int SortOrder { get; set; } = 0;
        public bool IsCardio { get; set; } = false;
        
        public WorkoutSessionLog WorkoutSessionLog { get; set; } = null!;
        public ICollection<LoggedSet> LoggedSets { get; set; } = new List<LoggedSet>();
    }
}
