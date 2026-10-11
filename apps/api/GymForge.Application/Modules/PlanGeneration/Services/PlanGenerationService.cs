using GymForge.Application.Modules.Diet.Interface;
using GymForge.Application.Modules.PlanGeneration.Interface;
using GymForge.Application.Modules.Workout.Interface;
using GymForge.Contracts.DietPlan;
using GymForge.Contracts.PlanGeneration;
using GymForge.Contracts.Workout;
using GymForge.Contracts.WorkoutPlan;
using GymForge.Shared.Constants;
using Microsoft.Extensions.Logging;

namespace GymForge.Application.Modules.PlanGeneration.Services
{
    public class PlanGenerationService : IPlanGenerationService
    {
        private readonly IWorkoutService _workoutService;
        private readonly IWorkoutPlanService _workoutPlanService;
        private readonly IMemberWorkoutService _memberWorkoutService;
        private readonly IDietPlanService _dietPlanService;
        private readonly IMemberDietService _memberDietService;
        private readonly ILogger<PlanGenerationService> _logger;

        private const int MinTrainingDays = 2;
        private const int MaxTrainingDays = 6;
        private const int DefaultSessionMinutes = 60;
        private const int MinutesPerExercise = 10;

        private static readonly string[] WeekDays = ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"];

        private static readonly Dictionary<int, string[]> TrainingWeekDays = new()
        {
            [2] = ["Monday", "Thursday"],
            [3] = ["Monday", "Wednesday", "Friday"],
            [4] = ["Monday", "Tuesday", "Thursday", "Friday"],
            [5] = ["Monday", "Tuesday", "Wednesday", "Friday", "Saturday"],
            [6] = ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"]
        };

        private static readonly Dictionary<int, string[][]> DaySplits = new()
        {
            [2] =
            [
                ["Chest", "Back", "Shoulders"],
                ["Legs", "Core"]
            ],
            [3] =
            [
                ["Chest", "Shoulders", "Triceps"],
                ["Back", "Biceps"],
                ["Legs", "Core"]
            ],
            [4] =
            [
                ["Chest", "Back", "Shoulders"],
                ["Legs", "Core"],
                ["Chest", "Shoulders", "Triceps"],
                ["Back", "Biceps"]
            ],
            [5] =
            [
                ["Chest", "Triceps"],
                ["Back", "Biceps"],
                ["Legs"],
                ["Shoulders", "Core"],
                ["Cardio", "Core"]
            ],
            [6] =
            [
                ["Chest", "Shoulders", "Triceps"],
                ["Back", "Biceps"],
                ["Legs"],
                ["Chest", "Shoulders", "Triceps"],
                ["Back", "Biceps"],
                ["Legs", "Core"]
            ]
        };

        private static readonly Dictionary<string, Dictionary<string, string>> MealSuggestions = new()
        {
            [DietTypes.Veg] = new()
            {
                ["Breakfast"] = "Poha or oats with milk, a handful of nuts, a serving of fruit",
                ["Lunch"] = "Paneer or dal, brown rice or roti, mixed vegetables, curd",
                ["Evening Snack"] = "Greek yogurt or roasted chana, a handful of nuts",
                ["Dinner"] = "Rajma, chole or tofu, salad, roti or quinoa"
            },
            [DietTypes.Eggetarian] = new()
            {
                ["Breakfast"] = "Eggs or an omelette, whole-grain toast, a serving of fruit",
                ["Lunch"] = "Paneer or dal, brown rice or roti, mixed vegetables, curd",
                ["Evening Snack"] = "Boiled eggs or Greek yogurt, a handful of nuts",
                ["Dinner"] = "Egg curry or tofu, salad, roti or quinoa"
            },
            [DietTypes.NonVeg] = new()
            {
                ["Breakfast"] = "Eggs or oats, whole-grain toast, a serving of fruit",
                ["Lunch"] = "Grilled chicken or fish, brown rice or roti, mixed vegetables",
                ["Evening Snack"] = "Greek yogurt or a protein shake, a handful of nuts",
                ["Dinner"] = "Grilled fish or chicken, salad, sweet potato or quinoa"
            },
            [DietTypes.Vegan] = new()
            {
                ["Breakfast"] = "Oats with soy milk, peanut butter, a serving of fruit",
                ["Lunch"] = "Tofu or dal, brown rice or roti, mixed vegetables",
                ["Evening Snack"] = "Roasted chana or a plant protein shake, a handful of nuts",
                ["Dinner"] = "Chole, rajma or tempeh, salad, quinoa or sweet potato"
            }
        };

        private static readonly Dictionary<string, string> MixedMealSuggestions = new()
        {
            ["Breakfast"] = "Oats or eggs, whole-grain toast, a serving of fruit",
            ["Lunch"] = "Grilled chicken, paneer or tofu, brown rice or roti, mixed vegetables",
            ["Evening Snack"] = "Greek yogurt or a protein shake, a handful of nuts",
            ["Dinner"] = "Grilled fish, chicken or legumes, salad, sweet potato or quinoa"
        };

        public PlanGenerationService(
            IWorkoutService workoutService,
            IWorkoutPlanService workoutPlanService,
            IMemberWorkoutService memberWorkoutService,
            IDietPlanService dietPlanService,
            IMemberDietService memberDietService,
            ILogger<PlanGenerationService> logger)
        {
            _workoutService = workoutService;
            _workoutPlanService = workoutPlanService;
            _memberWorkoutService = memberWorkoutService;
            _dietPlanService = dietPlanService;
            _memberDietService = memberDietService;
            _logger = logger;
        }

        public async Task<StarterPlanResultDto> GenerateStarterPlansAsync(Guid userId, StarterPlanProfileDto profile)
        {
            string goal = FitnessGoals.Normalize(profile.PrimaryGoal);
            string level = NormalizeLevel(profile.ExperienceLevel);
            MacroResult macros = CalculateMacros(profile, goal);

            try
            {
                int trainingDays = ResolveTrainingDays(profile.TrainingDaysPerWeek, level);
                int sessionMinutes = profile.SessionLengthMinutes is > 0 ? profile.SessionLengthMinutes.Value : DefaultSessionMinutes;
                CreateWorkoutPlanRequest workoutRequest = await BuildWorkoutPlanAsync(level, goal, trainingDays, sessionMinutes);
                if (workoutRequest.Days.Any(d => d.Exercises.Count > 0))
                {
                    WorkoutPlanDto workoutPlan = await _workoutPlanService.CreatePlanAsync(workoutRequest, null, userId);
                    await _memberWorkoutService.AssignPlanToMemberAsync(userId, workoutPlan.Id, null);
                }
                else
                {
                    _logger.LogWarning("Skipped starter workout plan generation for user {UserId}: no master exercises available.", userId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate starter workout plan for user {UserId}.", userId);
            }

            try
            {
                CreateDietPlanRequest dietRequest = BuildDietPlan(goal, DietTypes.Normalize(profile.DietPreference), macros);
                DietPlanDto dietPlan = await _dietPlanService.CreatePlanAsync(dietRequest, null, userId);
                await _memberDietService.AssignDietToMemberAsync(userId, dietPlan.Id, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate starter diet plan for user {UserId}.", userId);
            }

            return ToResult(macros);
        }

        public StarterPlanResultDto CalculateTargets(StarterPlanProfileDto profile) =>
            ToResult(CalculateMacros(profile, FitnessGoals.Normalize(profile.PrimaryGoal)));

        private static StarterPlanResultDto ToResult(MacroResult macros) => new()
        {
            TargetCalories = macros.Calories,
            TargetProtein = (int)Math.Round(macros.ProteinGrams),
            TargetCarbs = (int)Math.Round(macros.CarbGrams),
            TargetFats = (int)Math.Round(macros.FatGrams)
        };

        private static int ResolveTrainingDays(int? requested, string level)
        {
            if (requested.HasValue) return Math.Clamp(requested.Value, MinTrainingDays, MaxTrainingDays);
            return level switch
            {
                "Advanced" => 5,
                "Intermediate" => 4,
                _ => 3
            };
        }

        private async Task<CreateWorkoutPlanRequest> BuildWorkoutPlanAsync(string level, string goal, int trainingDays, int sessionMinutes)
        {
            string[][] split = DaySplits[trainingDays];
            string[] trainingWeekDays = TrainingWeekDays[trainingDays];
            (int Sets, string Reps) setsReps = GetSetsReps(goal);
            int exercisesPerSession = Math.Clamp(sessionMinutes / MinutesPerExercise, 3, 8);

            List<CreateWorkoutPlanDayDto> days = new();
            for (int dayIndex = 0; dayIndex < WeekDays.Length; dayIndex++)
            {
                string weekDay = WeekDays[dayIndex];
                int slot = Array.IndexOf(trainingWeekDays, weekDay);
                days.Add(slot < 0
                    ? new CreateWorkoutPlanDayDto { DayName = weekDay, DayIndex = dayIndex, IsRestDay = true }
                    : await BuildDayAsync(weekDay, dayIndex, split[slot], level, setsReps, exercisesPerSession));
            }

            return new CreateWorkoutPlanRequest
            {
                Name = $"{level} Starter Program",
                Description = $"Auto-generated {trainingDays}-day weekly plan based on your onboarding profile.",
                Level = level,
                Goal = MapGoalToWorkoutGoal(goal),
                Type = "Weekly",
                IsCustom = true,
                Days = days
            };
        }

        private async Task<CreateWorkoutPlanDayDto> BuildDayAsync(string dayName, int dayIndex, string[] categories, string level,
            (int Sets, string Reps) setsReps, int exercisesPerSession)
        {
            List<CreateWorkoutPlanExerciseDto> exercises = new();
            int perCategoryCount = Math.Max(1, (int)Math.Round((double)exercisesPerSession / categories.Length));
            int sortOrder = 0;

            foreach (string category in categories)
            {
                List<ExerciseDto> categoryExercises = await _workoutService.GetExercisesAsync(category, null, null);
                List<ExerciseDto> levelMatched = categoryExercises
                    .Where(e => string.Equals(e.Level, level, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                List<ExerciseDto> pool = levelMatched.Count > 0 ? levelMatched : categoryExercises;

                foreach (ExerciseDto exercise in pool.OrderBy(e => e.Name).Take(perCategoryCount))
                {
                    exercises.Add(new CreateWorkoutPlanExerciseDto
                    {
                        ExerciseId = exercise.Id,
                        ExerciseName = exercise.Name,
                        Sets = setsReps.Sets,
                        Reps = setsReps.Reps,
                        SortOrder = sortOrder++
                    });
                }
            }

            return new CreateWorkoutPlanDayDto
            {
                DayName = dayName,
                DayIndex = dayIndex,
                IsRestDay = false,
                Category = string.Join(" & ", categories),
                Exercises = exercises
            };
        }

        private static CreateDietPlanRequest BuildDietPlan(string goal, string? dietType, MacroResult macros)
        {
            Dictionary<string, string> suggestions = dietType != null ? MealSuggestions[dietType] : MixedMealSuggestions;
            List<CreateDietPlanMealDto> meals = new()
            {
                BuildMeal("Breakfast", "08:00 AM", 0.25m, macros, suggestions),
                BuildMeal("Lunch", "01:00 PM", 0.35m, macros, suggestions),
                BuildMeal("Evening Snack", "05:00 PM", 0.10m, macros, suggestions),
                BuildMeal("Dinner", "08:00 PM", 0.30m, macros, suggestions)
            };

            string suffix = dietType != null ? $" ({DietTypeLabel(dietType)})" : string.Empty;
            return new CreateDietPlanRequest
            {
                Name = $"{ToTitleCase(goal)} Starter Plan{suffix}",
                Description = "Auto-generated based on your onboarding profile.",
                Protein = macros.ProteinGrams,
                Carbs = macros.CarbGrams,
                Fats = macros.FatGrams,
                Goal = MapGoalToDietGoal(goal),
                DietType = dietType,
                IsCustom = true,
                IsTemplate = false,
                Meals = meals
            };
        }

        private static CreateDietPlanMealDto BuildMeal(string name, string time, decimal share, MacroResult macros, Dictionary<string, string> suggestions)
        {
            return new CreateDietPlanMealDto
            {
                Name = name,
                Time = time,
                Calories = (int)Math.Round(macros.Calories * share),
                Protein = Math.Round(macros.ProteinGrams * share, 1),
                Carbs = Math.Round(macros.CarbGrams * share, 1),
                Fats = Math.Round(macros.FatGrams * share, 1),
                Items = suggestions.GetValueOrDefault(name, "Balanced meal with protein, complex carbs, and vegetables"),
                SortOrder = 0
            };
        }

        private static string DietTypeLabel(string dietType) => dietType switch
        {
            DietTypes.Veg => "Veg",
            DietTypes.Eggetarian => "Eggetarian",
            DietTypes.Vegan => "Vegan",
            _ => "Non-veg"
        };

        private static (int Sets, string Reps) GetSetsReps(string goal) => goal switch
        {
            FitnessGoals.MuscleGain => (4, "8-10"),
            FitnessGoals.WeightLoss => (3, "12-15"),
            FitnessGoals.Endurance => (3, "15-20"),
            _ => (3, "10-12")
        };

        private static string MapGoalToWorkoutGoal(string goal) => goal switch
        {
            FitnessGoals.MuscleGain => "Hypertrophy",
            FitnessGoals.WeightLoss => "Fat Loss",
            FitnessGoals.Endurance => "Endurance",
            FitnessGoals.Flexibility => "Mobility",
            _ => "General Fitness"
        };

        private static string MapGoalToDietGoal(string goal) => goal switch
        {
            FitnessGoals.MuscleGain => "Muscle Gain",
            FitnessGoals.WeightLoss => "Weight Loss",
            FitnessGoals.Endurance => "Endurance",
            _ => "Maintenance"
        };

        private static string NormalizeLevel(string? level)
        {
            if (string.IsNullOrWhiteSpace(level)) return "Beginner";
            return level.Trim().ToLowerInvariant() switch
            {
                "intermediate" => "Intermediate",
                "advanced" => "Advanced",
                _ => "Beginner"
            };
        }

        private static string ToTitleCase(string goal)
        {
            string spaced = goal.Replace('_', ' ');
            return string.Join(' ', spaced.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(w => char.ToUpperInvariant(w[0]) + w[1..]));
        }

        private static int CalculateAge(DateTime dob)
        {
            DateTime today = DateTime.UtcNow.Date;
            int age = today.Year - dob.Year;
            if (dob.Date > today.AddYears(-age)) age--;
            return Math.Max(age, 0);
        }

        private static MacroResult CalculateMacros(StarterPlanProfileDto profile, string goal)
        {
            double h = (double)profile.HeightCm;
            double w = (double)profile.WeightKg;
            int age = CalculateAge(profile.DateOfBirth);
            string gender = profile.Gender ?? string.Empty;

            double bmr = gender.Equals("Male", StringComparison.OrdinalIgnoreCase)
                ? (10 * w) + (6.25 * h) - (5 * age) + 5
                : gender.Equals("Female", StringComparison.OrdinalIgnoreCase)
                    ? (10 * w) + (6.25 * h) - (5 * age) - 161
                    : (10 * w) + (6.25 * h) - (5 * age) - 78;

            double tdee = bmr * ActivityLevels.Multiplier(profile.ActivityLevel);

            double targetCalories = goal switch
            {
                FitnessGoals.WeightLoss => Math.Max(tdee - 500, bmr),
                FitnessGoals.MuscleGain => tdee + 300,
                _ => tdee
            };

            double proteinFactor = goal switch
            {
                FitnessGoals.MuscleGain => 2.2,
                FitnessGoals.WeightLoss => 2.0,
                FitnessGoals.Endurance => 1.6,
                _ => 1.8
            };

            double proteinGrams = w * proteinFactor;
            double proteinKcal = proteinGrams * 4;
            double fatKcal = targetCalories * 0.25;
            double fatGrams = fatKcal / 9;
            double carbKcal = Math.Max(targetCalories - proteinKcal - fatKcal, 0);
            double carbGrams = carbKcal / 4;

            return new MacroResult(
                (int)Math.Round(targetCalories),
                Math.Round((decimal)proteinGrams, 1),
                Math.Round((decimal)carbGrams, 1),
                Math.Round((decimal)fatGrams, 1)
            );
        }

        private record MacroResult(int Calories, decimal ProteinGrams, decimal CarbGrams, decimal FatGrams);
    }
}
