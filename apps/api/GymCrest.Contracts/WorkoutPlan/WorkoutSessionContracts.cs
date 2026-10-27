using System.ComponentModel.DataAnnotations;
namespace GymCrest.Contracts.WorkoutPlan
{
    public class WorkoutSessionLogDto
    {
        public Guid Id { get; set; }
        public Guid MemberId { get; set; }
        public DateTime Date { get; set; }
        public string DayName { get; set; } = string.Empty;
        public int ExercisesCompleted { get; set; }
        public int TotalSets { get; set; }
        public string Status { get; set; } = "Completed";
        [MaxLength(2000)]
        public string? Notes { get; set; }
        public List<LoggedExerciseDto> LoggedExercises { get; set; } = [];
    }

    public class LoggedExerciseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool Skipped { get; set; } = false;
        public int SortOrder { get; set; }
        public bool IsCardio { get; set; } = false;
        public List<LoggedSetDto> LoggedSets { get; set; } = [];
    }

    public class LoggedSetDto
    {
        public Guid Id { get; set; }
        public int SetNo { get; set; }
        public double Weight { get; set; }
        public int Reps { get; set; }
        public bool Completed { get; set; }
    }

    public class LogWorkoutSessionRequest
    {
        public DateTime Date { get; set; }
        public string DayName { get; set; } = string.Empty;
        public string Status { get; set; } = "Completed";
        [MaxLength(2000)]
        public string? Notes { get; set; }
        public List<LogLoggedExerciseDto> LoggedExercises { get; set; } = [];
    }

    public class LogLoggedExerciseDto
    {
        public string Name { get; set; } = string.Empty;
        public bool Skipped { get; set; } = false;
        public int SortOrder { get; set; }
        public bool IsCardio { get; set; } = false;
        public List<LogLoggedSetDto> LoggedSets { get; set; } = [];
    }

    public class LogLoggedSetDto
    {
        public int SetNo { get; set; }
        public double Weight { get; set; }
        public int Reps { get; set; }
        public bool Completed { get; set; }
    }

    public class AssignWorkoutPlanRequest
    {
        public Guid WorkoutPlanId { get; set; }
    }

    public class SaveRecurringOverrideRequest
    {
        public string DayOfWeek { get; set; } = string.Empty;
        public Guid? WorkoutPlanDayId { get; set; }
        public bool IsRestDay { get; set; }
    }
}
