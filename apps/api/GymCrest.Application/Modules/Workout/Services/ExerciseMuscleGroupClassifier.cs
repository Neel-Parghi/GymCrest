using System.Text.RegularExpressions;

namespace GymCrest.Application.Modules.Workout.Services
{
    /// <summary>
    /// Best-effort muscle group for a free-text exercise name (e.g. "Pec deck machine", "Barbell Bench Press - Flat"),
    /// used when the name isn't in any exercise library. Rules are checked in order, so more specific
    /// patterns ("leg curl", "rear delt fly", "close grip bench") win over generic ones ("curl", "fly", "bench").
    /// </summary>
    public static class ExerciseMuscleGroupClassifier
    {
        private static readonly (string Group, Regex Pattern)[] Rules =
        [
            ("Core", Build(@"leg raises?", @"knee raises?", "crunch(es)?", "planks?", @"sit[\s-]?ups?", "abs?", "oblique", @"russian twist", @"ab wheel", @"mountain climbers?", @"dead ?bugs?")),
            ("Triceps", Build("triceps?", @"push[\s-]?downs?", "skull ?crushers?", "kickbacks?", @"close[\s-]?grip (bench|press)", "dips?", @"overhead (triceps? )?extensions?", "jm press")),
            ("Legs", Build("legs?", "squats?", "lunges?", "calf", "calves", "hamstrings?", "quads?", "glutes?", @"hip thrusts?", "romanian", "rdl", @"step[\s-]?ups?", "hack", "adductors?", "abductors?", @"good mornings?", @"bulgarian")),
            ("Shoulders", Build("shoulders?", @"overhead press", "ohp", "military", @"lateral raises?", @"side raises?", @"front raises?", @"rear delts?", "delts?", "arnold", @"upright rows?", @"face pulls?", @"reverse fl(y|ies|ye)s?")),
            ("Biceps", Build("biceps?", "curls?", "hammer", "preacher")),
            ("Chest", Build("bench", "chest", "pecs?", "pec deck", "fl(y|ies|ye|yes)", @"push[\s-]?ups?", "crossovers?", @"incline (dumbbell |db |barbell )?press", @"decline (dumbbell |db |barbell )?press", @"cable press")),
            ("Back", Build("rows?", @"pull[\s-]?downs?", @"pull[\s-]?ups?", @"chin[\s-]?ups?", "lats?", "deadlifts?", @"back extensions?", "hyperextensions?", "shrugs?", @"t[\s-]?bar"))
        ];

        public static string? Classify(string exerciseName)
        {
            if (string.IsNullOrWhiteSpace(exerciseName))
                return null;

            string name = exerciseName.Trim().ToLowerInvariant();
            foreach ((string group, Regex pattern) in Rules)
            {
                if (pattern.IsMatch(name))
                    return group;
            }
            return null;
        }

        private static Regex Build(params string[] alternatives) =>
            new($@"\b({string.Join("|", alternatives)})\b", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    }
}
