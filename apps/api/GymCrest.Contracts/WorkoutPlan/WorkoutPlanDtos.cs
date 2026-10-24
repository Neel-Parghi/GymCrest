namespace GymCrest.Contracts.WorkoutPlan
{
    public class WorkoutPlanDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Level { get; set; } = "Beginner";
        public string Goal { get; set; } = "Hypertrophy";
        public string Type { get; set; } = "Split";
        public Guid? GymId { get; set; }
        public int DaysCount { get; set; }
        public int ExercisesCount { get; set; }
        public bool IsCustom { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public List<WorkoutPlanDayDto> Days { get; set; } = [];
    }

    public class WorkoutPlanDayDto
    {
        public Guid Id { get; set; }
        public Guid WorkoutPlanId { get; set; }
        public string? DayName { get; set; }
        public int DayIndex { get; set; }
        public bool IsRestDay { get; set; }
        public string? Category { get; set; }
        public string? FreeTextNotes { get; set; }
        public List<WorkoutPlanExerciseDto> Exercises { get; set; } = [];
    }

    public class WorkoutPlanExerciseDto
    {
        public Guid Id { get; set; }
        public Guid WorkoutPlanDayId { get; set; }
        public Guid? ExerciseId { get; set; }
        public string ExerciseName { get; set; } = string.Empty;
        public int Sets { get; set; }
        public string Reps { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public int SortOrder { get; set; }
    }

    public class CreateWorkoutPlanRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Level { get; set; } = "Beginner";
        public string Goal { get; set; } = "Hypertrophy";
        public string Type { get; set; } = "Split";
        public bool IsCustom { get; set; } = false;
        public List<CreateWorkoutPlanDayDto> Days { get; set; } = [];
    }

    public class UpdateWorkoutPlanRequest
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Level { get; set; } = "Beginner";
        public string Goal { get; set; } = "Hypertrophy";
        public string Type { get; set; } = "Split";
        public bool IsCustom { get; set; } = false;
        public List<CreateWorkoutPlanDayDto> Days { get; set; } = new List<CreateWorkoutPlanDayDto>();
    }

    public class CreateWorkoutPlanDayDto
    {
        public string? DayName { get; set; }
        public int DayIndex { get; set; }
        public bool IsRestDay { get; set; }
        public string? Category { get; set; }
        public string? FreeTextNotes { get; set; }
        public List<CreateWorkoutPlanExerciseDto> Exercises { get; set; } = [];
    }

    public class CreateWorkoutPlanExerciseDto
    {
        public Guid? ExerciseId { get; set; }
        public string ExerciseName { get; set; } = string.Empty;
        public int Sets { get; set; }
        public string Reps { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public int SortOrder { get; set; }
    }

    public class MemberPlanAssignmentDto
    {
        public Guid Id { get; set; }
        public Guid? MemberId { get; set; }
        public Guid? UserId { get; set; }
        public Guid WorkoutPlanId { get; set; }
        public DateTime AssignedAt { get; set; }
        public bool IsActive { get; set; }
        public WorkoutPlanDto WorkoutPlan { get; set; } = null!;
    }
}
