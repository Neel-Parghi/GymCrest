namespace GymCrest.Contracts.PlanGeneration
{
    public class StarterPlanProfileDto
    {
        public string? PrimaryGoal { get; set; }
        public string? ExperienceLevel { get; set; }
        public string? ActivityLevel { get; set; }
        public string? DietPreference { get; set; }
        public int? TrainingDaysPerWeek { get; set; }
        public int? SessionLengthMinutes { get; set; }
        public decimal HeightCm { get; set; }
        public decimal WeightKg { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string Gender { get; set; } = string.Empty;
    }
}
