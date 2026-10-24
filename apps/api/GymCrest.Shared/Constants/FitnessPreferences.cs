namespace GymCrest.Shared.Constants
{
    public static class FitnessGoals
    {
        public const string WeightLoss = "weight_loss";
        public const string MuscleGain = "muscle_gain";
        public const string Endurance = "endurance";
        public const string Flexibility = "flexibility";
        public const string GeneralFitness = "general_fitness";

        private static readonly string[] All = [WeightLoss, MuscleGain, Endurance, Flexibility, GeneralFitness];

        public static string Normalize(string? goal)
        {
            string id = ToId(goal);
            return All.Contains(id) ? id : GeneralFitness;
        }

        internal static string ToId(string? value) =>
            (value ?? string.Empty).Trim().ToLowerInvariant().Replace(' ', '_').Replace('-', '_');
    }

    public static class DietTypes
    {
        public const string Veg = "veg";
        public const string Eggetarian = "eggetarian";
        public const string NonVeg = "non_veg";
        public const string Vegan = "vegan";

        private static readonly string[] All = [Veg, Eggetarian, NonVeg, Vegan];

        public static string? Normalize(string? dietType)
        {
            string id = FitnessGoals.ToId(dietType);
            if (id == "nonveg") id = NonVeg;
            return All.Contains(id) ? id : null;
        }
    }

    public static class ActivityLevels
    {
        public const string Sedentary = "sedentary";
        public const string Light = "light";
        public const string Moderate = "moderate";
        public const string Active = "active";
        public const string VeryActive = "very_active";

        private static readonly Dictionary<string, double> Multipliers = new()
        {
            [Sedentary] = 1.2,
            [Light] = 1.375,
            [Moderate] = 1.55,
            [Active] = 1.725,
            [VeryActive] = 1.9
        };

        public static string? Normalize(string? level)
        {
            string id = FitnessGoals.ToId(level);
            return Multipliers.ContainsKey(id) ? id : null;
        }

        public static double Multiplier(string? level) =>
            Multipliers.GetValueOrDefault(Normalize(level) ?? Moderate);
    }
}
