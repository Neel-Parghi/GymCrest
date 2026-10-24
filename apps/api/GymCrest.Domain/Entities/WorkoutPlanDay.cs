namespace GymCrest.Domain.Entities
{
    public class WorkoutPlanDay : BaseEntity
    {
        public Guid WorkoutPlanId { get; set; }
        
        public WorkoutPlan? WorkoutPlan { get; set; }

        public string? DayName { get; set; }
        
        public int DayIndex { get; set; }
        
        public bool IsRestDay { get; set; } = false;
        
        public string? Category { get; set; }
        
        public string? FreeTextNotes { get; set; }

        public ICollection<WorkoutPlanExercise> Exercises { get; set; } = [];
    }
}
