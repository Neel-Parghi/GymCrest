using GymCrest.Application.Modules.Diet.Interfaces;
using GymCrest.Application.Modules.PlanGeneration.Interface;
using GymCrest.Application.Modules.SuperAdmin.Interfaces;
using GymCrest.Application.Modules.Workout.Interface;
using GymCrest.Contracts.DietTracking;
using GymCrest.Contracts.PlanGeneration;
using GymCrest.Contracts.SuperAdmin;
using GymCrest.Contracts.WorkoutPlan;
using GymCrest.Domain.Entities;
using GymCrest.Domain.Interface;
using GymCrest.Shared.Constants;
using GymCrest.Shared.Enums;
using GymCrest.Shared.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GymCrest.Application.Modules.SuperAdmin.Services
{
    /// <summary>
    /// Rebuilds the public demo member so visitors see a lived-in account. Logs go through the same
    /// services the member portal uses, so totals, dates and streaks match real usage.
    /// </summary>
    public class DemoDataService : IDemoDataService
    {
        private const string DefaultDemoMemberEmail = "demouser@gymcrest.com";
        private const int DaysOfHistory = 14;
        // A missed training day early in the window keeps the history realistic without breaking the current streak.
        private const int MissedTrainingDayOffset = 12;

        private const double HeightCm = 175;
        private const double StartWeightKg = 78.6;
        private const double TargetWeightKg = 74;
        private static readonly DateTime DateOfBirth = new(1998, 3, 14, 0, 0, 0, DateTimeKind.Utc);

        private static readonly string[] CardioKeywords = ["run", "jog", "cycl", "bike", "treadmill", "elliptical", "rowing", "jump rope", "skipping", "stair"];
        private static readonly string[] BodyweightKeywords = ["push-up", "push up", "pushup", "pull-up", "pull up", "pullup", "chin-up", "plank", "crunch", "sit-up", "dip", "burpee", "mountain climber", "bridge", "superman", "lunge"];
        private static readonly string[] HeavyKeywords = ["squat", "deadlift", "leg press", "hip thrust"];
        private static readonly string[] MediumKeywords = ["bench", "row", "press", "pulldown", "pull-down"];

        private readonly IDemoDataRepository _demoDataRepository;
        private readonly IPlanGenerationService _planGenerationService;
        private readonly IMemberWorkoutService _memberWorkoutService;
        private readonly IDietTrackingService _dietTrackingService;
        private readonly IMemberDietRepository _memberDietRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _config;
        private readonly ILogger<DemoDataService> _logger;

        public DemoDataService(
            IDemoDataRepository demoDataRepository,
            IPlanGenerationService planGenerationService,
            IMemberWorkoutService memberWorkoutService,
            IDietTrackingService dietTrackingService,
            IMemberDietRepository memberDietRepository,
            IUnitOfWork unitOfWork,
            IConfiguration config,
            ILogger<DemoDataService> logger)
        {
            _demoDataRepository = demoDataRepository;
            _planGenerationService = planGenerationService;
            _memberWorkoutService = memberWorkoutService;
            _dietTrackingService = dietTrackingService;
            _memberDietRepository = memberDietRepository;
            _unitOfWork = unitOfWork;
            _config = config;
            _logger = logger;
        }

        public async Task<DemoDataResetResultDto> ResetDemoMemberAsync()
        {
            string email = _config["Demo:MemberEmail"] ?? DefaultDemoMemberEmail;
            User user = await _demoDataRepository.GetUserByEmailAsync(email)
                ?? throw new Exception($"Demo member {email} was not found.");

            if (user.Role != UserRole.User)
            {
                throw new Exception($"{email} is not a member account, so it can't be reset as the demo member.");
            }

            // Fixed seed: every reset produces the same believable history.
            Random random = new(1912);
            DateTime today = AppTimeZone.Today;
            DemoDataResetResultDto result = new() { Email = email };

            await _demoDataRepository.ClearMemberActivityAsync(user.Id);
            await PrepareProfileAsync(user);
            await GeneratePlansAsync(user);

            await LogWorkoutsAsync(user.Id, today, random, result);
            await LogMealsAsync(user.Id, today, random, result);
            await AddHealthCheckInsAsync(user.Id, today, result);

            _logger.LogInformation("Demo member {Email} reset: {Workouts} workouts, {MealDays} days of meals, {CheckIns} check-ins.",
                email, result.WorkoutsLogged, result.DaysOfMealsLogged, result.HealthCheckIns);
            return result;
        }

        private async Task PrepareProfileAsync(User user)
        {
            user.Profile ??= new UserProfile { UserId = user.Id };
            user.Profile.DateOfBirth = DateOfBirth;
            user.Profile.Gender = Gender.Male;
            user.Profile.IsOnboarded = true;

            user.Preference ??= new UserPreference { UserId = user.Id };
            user.Preference.PrimaryGoal = FitnessGoals.WeightLoss;
            user.Preference.ExperienceLevel = "Intermediate";
            user.Preference.ActivityLevel = ActivityLevels.Moderate;
            user.Preference.DietPreference = DietTypes.Veg;
            user.Preference.TrainingDaysPerWeek = 4;
            user.Preference.TargetTrainingTime = 60;
            user.Preference.TargetWeight = TargetWeightKg;
            user.Preference.TargetWeightStart = StartWeightKg;

            await _unitOfWork.SaveChangesAsync();
        }

        private async Task GeneratePlansAsync(User user)
        {
            StarterPlanResultDto targets = await _planGenerationService.GenerateStarterPlansAsync(user.Id, new StarterPlanProfileDto
            {
                PrimaryGoal = user.Preference!.PrimaryGoal,
                ExperienceLevel = user.Preference.ExperienceLevel,
                ActivityLevel = user.Preference.ActivityLevel,
                DietPreference = user.Preference.DietPreference,
                TrainingDaysPerWeek = user.Preference.TrainingDaysPerWeek,
                SessionLengthMinutes = user.Preference.TargetTrainingTime,
                HeightCm = (decimal)HeightCm,
                WeightKg = (decimal)StartWeightKg,
                DateOfBirth = DateOfBirth,
                Gender = nameof(Gender.Male)
            });

            user.Preference.TargetCalories = targets.TargetCalories;
            user.Preference.TargetProtein = targets.TargetProtein;
            user.Preference.TargetCarbs = targets.TargetCarbs;
            user.Preference.TargetFats = targets.TargetFats;
            await _unitOfWork.SaveChangesAsync();
        }

        private async Task LogWorkoutsAsync(Guid userId, DateTime today, Random random, DemoDataResetResultDto result)
        {
            List<WorkoutPlanDay> planDays = await _demoDataRepository.GetActivePlanDaysAsync(userId);
            if (planDays.Count == 0)
            {
                _logger.LogWarning("Demo reset: no workout plan was generated, so no workouts were logged.");
                return;
            }

            for (int offset = DaysOfHistory; offset >= 1; offset--)
            {
                DateTime day = today.AddDays(-offset);
                WorkoutPlanDay? planDay = planDays.FirstOrDefault(d =>
                    string.Equals(d.DayName, day.DayOfWeek.ToString(), StringComparison.OrdinalIgnoreCase));
                if (planDay == null) continue;

                bool isRest = planDay.IsRestDay || planDay.Exercises.Count == 0;
                if (!isRest && offset == MissedTrainingDayOffset) continue;

                // Lifts creep up week over week, the way real progress looks.
                double progression = 1 + (DaysOfHistory - offset) / 7 * 0.05;

                LogWorkoutSessionRequest request = new()
                {
                    Date = AppTimeZone.DayBoundsUtc(day).StartUtc,
                    DayName = planDay.DayName ?? day.DayOfWeek.ToString(),
                    Status = isRest ? "RestDay" : "Completed",
                    Notes = isRest ? "Rest Day logged from tracker" : string.Empty,
                    LoggedExercises = isRest ? [] : [.. planDay.Exercises
                        .OrderBy(e => e.SortOrder)
                        .Select((e, index) => BuildLoggedExercise(e, index, progression, random))]
                };

                await _memberWorkoutService.LogWorkoutSessionAsync(userId, request, null);
                if (isRest) result.RestDaysLogged++;
                else result.WorkoutsLogged++;
            }
        }

        private static LogLoggedExerciseDto BuildLoggedExercise(WorkoutPlanExercise exercise, int index, double progression, Random random)
        {
            string name = exercise.ExerciseName;
            string lower = name.ToLowerInvariant();

            if (CardioKeywords.Any(lower.Contains))
            {
                return new LogLoggedExerciseDto
                {
                    Name = name,
                    SortOrder = index,
                    IsCardio = true,
                    LoggedSets = [new LogLoggedSetDto { SetNo = 1, Weight = 0, Reps = 15 + random.Next(0, 11), Completed = true }]
                };
            }

            (int minReps, int maxReps) = ParseRepRange(exercise.Reps);
            bool bodyweight = BodyweightKeywords.Any(lower.Contains);
            double baseWeight = bodyweight ? 0
                : HeavyKeywords.Any(lower.Contains) ? 60
                : MediumKeywords.Any(lower.Contains) ? 35
                : 12;
            double weight = bodyweight ? 0 : RoundToPlate(baseWeight * progression);
            int sets = Math.Max(exercise.Sets, 1);

            List<LogLoggedSetDto> loggedSets = [];
            for (int setNo = 1; setNo <= sets; setNo++)
            {
                // Fatigue: the last set usually comes in a rep or two short.
                int reps = setNo == sets
                    ? Math.Max(minReps - random.Next(0, 2), 1)
                    : random.Next(minReps, maxReps + 1);
                loggedSets.Add(new LogLoggedSetDto { SetNo = setNo, Weight = weight, Reps = reps, Completed = true });
            }

            return new LogLoggedExerciseDto { Name = name, SortOrder = index, LoggedSets = loggedSets };
        }

        private static (int Min, int Max) ParseRepRange(string? reps)
        {
            string[] parts = (reps ?? string.Empty).Split('-', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            int min = parts.Length > 0 && int.TryParse(parts[0], out int a) ? a : 10;
            int max = parts.Length > 1 && int.TryParse(parts[1], out int b) ? b : min;
            return (Math.Min(min, max), Math.Max(min, max));
        }

        private static double RoundToPlate(double kg) => Math.Round(kg / 2.5) * 2.5;

        private async Task LogMealsAsync(Guid userId, DateTime today, Random random, DemoDataResetResultDto result)
        {
            MemberDietAssignment? assignment = await _memberDietRepository.GetActiveDietAssignmentAsync(userId);
            List<DietPlanMeal> meals = assignment?.DietPlan?.Meals.OrderBy(m => m.SortOrder).ThenBy(m => m.Time).ToList() ?? [];
            if (meals.Count == 0)
            {
                _logger.LogWarning("Demo reset: no diet plan was generated, so no meals were logged.");
                return;
            }

            for (int offset = DaysOfHistory; offset >= 1; offset--)
            {
                DateTime day = DateTime.SpecifyKind(today.AddDays(-offset), DateTimeKind.Utc);

                foreach (DietPlanMeal meal in meals)
                {
                    bool isSnack = meal.Name.Contains("Snack", StringComparison.OrdinalIgnoreCase);
                    if (isSnack && offset % 3 == 0)
                    {
                        await AddMealAsync(userId, day, "Custom", "Masala chai and 2 digestive biscuits", 180, 4, 26, 6, null);
                    }
                    else
                    {
                        // Real portions drift a little from the plan.
                        decimal factor = 0.9m + (decimal)random.NextDouble() * 0.18m;
                        await AddMealAsync(userId, day, "Assigned", string.IsNullOrWhiteSpace(meal.Items) ? meal.Name : meal.Items,
                            (int)Math.Round(meal.Calories * factor), meal.Protein * factor, meal.Carbs * factor, meal.Fats * factor, meal.Id);
                    }
                    result.MealEntriesLogged++;
                }

                if (offset % 4 == 1)
                {
                    await AddMealAsync(userId, day, "Custom", "Whey protein shake with water", 120, 24, 3, 1.5m, null);
                    result.MealEntriesLogged++;
                }

                result.DaysOfMealsLogged++;
            }
        }

        private Task AddMealAsync(Guid userId, DateTime day, string mealType, string foodName,
            int calories, decimal protein, decimal carbs, decimal fats, Guid? sourceMealId)
        {
            return _dietTrackingService.AddMealEntryAsync(userId, new AddMealEntryRequestDto
            {
                LogDate = day,
                MealType = mealType,
                FoodName = foodName,
                Calories = calories,
                Protein = Math.Round(protein, 1),
                Carbs = Math.Round(carbs, 1),
                Fats = Math.Round(fats, 1),
                SourceDietPlanMealId = sourceMealId
            });
        }

        private async Task AddHealthCheckInsAsync(Guid userId, DateTime today, DemoDataResetResultDto result)
        {
            // Roughly every 9 days over the past month, trending toward the target weight.
            (int DaysAgo, double Weight, double BodyFat, double Waist, bool Advanced)[] checkIns =
            [
                (28, StartWeightKg, 21.8, 89.5, true),
                (19, 77.7, 21.1, 88.4, false),
                (10, 76.9, 20.5, 87.2, true),
                (1, 76.2, 19.9, 86.3, false)
            ];

            List<MemberMeasurement> measurements = [.. checkIns.Select(c =>
            {
                double heightM = HeightCm / 100;
                return new MemberMeasurement
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Weight = c.Weight,
                    Height = HeightCm,
                    BMI = Math.Round(c.Weight / (heightM * heightM), 1),
                    BodyFatPercentage = c.BodyFat,
                    IsAdvanced = c.Advanced,
                    Waist = c.Advanced ? c.Waist : null,
                    Chest = c.Advanced ? Math.Round(99.0 - (28 - c.DaysAgo) * 0.04, 1) : null,
                    Hips = c.Advanced ? Math.Round(97.5 - (28 - c.DaysAgo) * 0.05, 1) : null,
                    LeftBicep = c.Advanced ? 34.5 : null,
                    RightBicep = c.Advanced ? 35.0 : null,
                    LeftThigh = c.Advanced ? 56.0 : null,
                    RightThigh = c.Advanced ? 56.5 : null,
                    Notes = c.Advanced ? "Monthly check-in with full measurements" : null,
                    // 7:30 AM IST on the check-in day.
                    Date = AppTimeZone.DayBoundsUtc(today.AddDays(-c.DaysAgo)).StartUtc.AddHours(7.5),
                    CreatedOn = DateTime.UtcNow,
                    CreatedBy = userId
                };
            })];

            await _demoDataRepository.AddMeasurementsAsync(measurements);
            await _unitOfWork.SaveChangesAsync();
            result.HealthCheckIns = measurements.Count;
        }
    }
}
