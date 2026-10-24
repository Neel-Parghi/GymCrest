namespace GymCrest.Contracts.Workout
{
    public class ExerciseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string? SubCategory { get; set; }
        public string Equipment { get; set; } = string.Empty;
        public string? Force { get; set; }
        public string Level { get; set; } = "Beginner";
        public string? Description { get; set; }
        public string? Instructions { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsCustom { get; set; }
    }

    public class ExerciseFilterParams
    {
        public string? Category { get; set; }
        public string? Equipment { get; set; }
        public string? Search { get; set; }
    }
}
