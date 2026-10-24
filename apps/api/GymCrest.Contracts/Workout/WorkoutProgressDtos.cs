namespace GymCrest.Contracts.Workout
{
    public class LoggedExerciseNameDto
    {
        public string Name { get; set; } = string.Empty;
        public string? MuscleGroup { get; set; }
        public DateTime LastLoggedDate { get; set; }
    }

    public class ProgressSetDto
    {
        public int SetNo { get; set; }
        public double Weight { get; set; }
        public int Reps { get; set; }
    }

    public class ExerciseProgressPointDto
    {
        public Guid SessionLogId { get; set; }
        public DateTime Date { get; set; }
        public string DayName { get; set; } = string.Empty;
        public double TopWeight { get; set; }
        public int TopWeightReps { get; set; }
        public int TotalSets { get; set; }
        public int TotalReps { get; set; }
        public double Volume { get; set; }
        public List<ProgressSetDto> Sets { get; set; } = [];
    }

    public class ExerciseProgressDto
    {
        public string ExerciseName { get; set; } = string.Empty;
        public string? MuscleGroup { get; set; }
        public double PersonalBest { get; set; }
        public int TotalSessions { get; set; }
        public DateTime? LastLoggedDate { get; set; }
        public double? EstimatedOneRepMax { get; set; }
        public List<ExerciseProgressPointDto> Points { get; set; } = [];
    }

    public class MuscleGroupExerciseDto
    {
        public string Name { get; set; } = string.Empty;
        public int TotalSessions { get; set; }
        public ExerciseProgressPointDto Last { get; set; } = new();
        public ExerciseProgressPointDto? Previous { get; set; }
        public bool IsNewPersonalBest { get; set; }
        public List<double> Trend { get; set; } = [];
    }

    public class MuscleGroupProgressDto
    {
        public string MuscleGroup { get; set; } = string.Empty;
        public int WeeklySessions { get; set; }
        public int WeeklySets { get; set; }
        public double WeeklyVolume { get; set; }
        public double PreviousWeeklyVolume { get; set; }
        public List<MuscleGroupExerciseDto> Exercises { get; set; } = [];
    }
}
