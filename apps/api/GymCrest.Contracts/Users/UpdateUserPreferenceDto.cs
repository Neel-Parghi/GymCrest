namespace GymCrest.Contracts.Users
{
    public class UpdateUserPreferenceDto
    {
        public string? PrimaryGoal { get; set; }
        public double? TargetWeight { get; set; }
        public int? TargetCalories { get; set; }
        public int? TargetProtein { get; set; }
        public int? TargetCarbs { get; set; }
        public int? TargetFats { get; set; }
        public int? TargetTrainingTime { get; set; }
        public string? DietPreference { get; set; }
        public string? ActivityLevel { get; set; }
        public int? TrainingDaysPerWeek { get; set; }

        public bool? EmailNotificationsEnabled { get; set; }
        public bool? ShowGymBuddy { get; set; }
    }

    public class UserPreferenceDto : UpdateUserPreferenceDto
    {
    }
}
