using System.ComponentModel.DataAnnotations;
namespace GymCrest.Contracts.Users
{
    public class CompleteUserOnboardingDto
    {
        public string PrimaryGoal { get; set; } = string.Empty;
        public string? ExperienceLevel { get; set; }
        public decimal Height { get; set; }
        public decimal Weight { get; set; }
        public decimal? TargetWeight { get; set; }
        public DateTime DOB { get; set; }
        public string Gender { get; set; } = string.Empty;
        public string? ActivityLevel { get; set; }
        public string? DietPreference { get; set; }
        public int? TrainingDaysPerWeek { get; set; }
        public int? SessionLengthMinutes { get; set; }
        [MaxLength(2000)]
        public string? Limitations { get; set; }
    }
}
