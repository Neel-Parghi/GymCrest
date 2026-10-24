namespace GymCrest.Domain.Entities
{
    public class WorkoutPlanExercise : BaseEntity
    {
        public Guid WorkoutPlanDayId { get; set; }
        
        public WorkoutPlanDay? WorkoutPlanDay { get; set; }

        public Guid? ExerciseId { get; set; }
        
        public Exercise? Exercise { get; set; }

        public string ExerciseName { get; set; } = string.Empty;
        
        public int Sets { get; set; }
        
        public string Reps { get; set; } = string.Empty;
        
        public string? Notes { get; set; }
        
        public int SortOrder { get; set; }
    }
}
