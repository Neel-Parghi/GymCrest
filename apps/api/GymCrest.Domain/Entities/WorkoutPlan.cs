namespace GymCrest.Domain.Entities
{
    public class WorkoutPlan : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        
        public string Description { get; set; } = string.Empty;
        
        public string Level { get; set; } = "Beginner";
        
        public string Goal { get; set; } = "Hypertrophy";   
        
        public string Type { get; set; } = "Split";
        public Guid? GymId { get; set; }
        
        public int DaysCount { get; set; }
        
        public int ExercisesCount { get; set; }

        public bool IsDeleted { get; set; } = false;
        
        // False = visible to all gym members. True = personal PT plan, visible only to assigned members.
        public bool IsCustom { get; set; } = false;
        
        public DateTime? DeletedAt { get; set; }

        public ICollection<WorkoutPlanDay> Days { get; set; } =  [];
    }
}
