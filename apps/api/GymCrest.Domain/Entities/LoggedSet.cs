namespace GymCrest.Domain.Entities
{
    public class LoggedSet : BaseEntity
    {
        public Guid LoggedExerciseId { get; set; }
        public int SetNo { get; set; }
        public double Weight { get; set; }
        public int Reps { get; set; }
        public bool Completed { get; set; }
        
        public LoggedExercise LoggedExercise { get; set; } = null!;
    }
}
