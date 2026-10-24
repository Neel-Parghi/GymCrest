using GymCrest.Application.Modules.Workout.Interface;
using GymCrest.Contracts.Workout;
using GymCrest.Domain.Entities;
using GymCrest.Domain.Interface;

namespace GymCrest.Application.Modules.Workout.Services
{
    public class UserWorkoutProgressService : IUserWorkoutProgressService
    {
        private const int WeeklyWindowDays = 7;
        private const int TrendLength = 6;
        private const string UncategorizedGroup = "Other";

        private readonly IMemberWorkoutRepository _memberWorkoutRepository;
        private readonly IWorkoutRepository _workoutRepository;

        public UserWorkoutProgressService(IMemberWorkoutRepository memberWorkoutRepository, IWorkoutRepository workoutRepository)
        {
            _memberWorkoutRepository = memberWorkoutRepository;
            _workoutRepository = workoutRepository;
        }

        public async Task<IEnumerable<LoggedExerciseNameDto>> GetLoggedExerciseNamesAsync(Guid userId)
        {
            IEnumerable<WorkoutSessionLog> logs = await _memberWorkoutRepository.GetWorkoutLogsAsync(userId);

            List<(DateTime Date, LoggedExercise Exercise)> trackable = [.. logs
                .SelectMany(l => l.LoggedExercises.Select(e => (l.Date, Exercise: e)))
                .Where(x => !x.Exercise.Skipped && !x.Exercise.IsCardio && x.Exercise.LoggedSets.Any(s => s.Completed))];

            List<IGrouping<string, (DateTime Date, LoggedExercise Exercise)>> grouped = [.. trackable
                .GroupBy(x => x.Exercise.Name.Trim().ToLower())];

            Dictionary<string, string> categoryMap = await _workoutRepository.GetCategoriesForNamesAsync(
                grouped.Select(g => g.Key));

            List<LoggedExerciseNameDto> result = [.. grouped
                .Select(g =>
                {
                    (DateTime Date, LoggedExercise Exercise) latest = g.OrderByDescending(x => x.Date).First();

                    return new LoggedExerciseNameDto
                    {
                        Name = latest.Exercise.Name.Trim(),
                        MuscleGroup = ResolveMuscleGroup(latest.Exercise.Name, categoryMap),
                        LastLoggedDate = latest.Date
                    };
                })
                .OrderByDescending(x => x.LastLoggedDate)];

            return result;
        }

        public async Task<ExerciseProgressDto?> GetExerciseProgressAsync(Guid userId, string exerciseName)
        {
            if (string.IsNullOrWhiteSpace(exerciseName))
                return null;

            IEnumerable<WorkoutSessionLog> logs = await _memberWorkoutRepository.GetWorkoutLogsAsync(userId);
            string target = NormalizeName(exerciseName);

            List<ExerciseProgressPointDto> points = [.. GetTrackedSessions(logs)
                .Where(x => NormalizeName(x.Exercise.Name) == target)
                .Select(x => ToPoint(x.Log, x.Exercise))
                .OrderBy(p => p.Date)];

            if (points.Count == 0)
                return null;

            ExerciseProgressPointDto mostRecent = points[^1];
            double? oneRepMax = mostRecent.TopWeightReps > 0
                ? Math.Round(mostRecent.TopWeight * (1 + mostRecent.TopWeightReps / 30.0), 1)
                : null;

            Dictionary<string, string> categoryMap = await _workoutRepository.GetCategoriesForNamesAsync([target]);
            string? category = ResolveMuscleGroup(target, categoryMap);

            return new ExerciseProgressDto
            {
                ExerciseName = exerciseName.Trim(),
                MuscleGroup = category,
                PersonalBest = points.Max(p => p.TopWeight),
                TotalSessions = points.Count,
                LastLoggedDate = mostRecent.Date,
                EstimatedOneRepMax = oneRepMax,
                Points = points
            };
        }

        public async Task<IEnumerable<MuscleGroupProgressDto>> GetMuscleGroupProgressAsync(Guid userId)
        {
            IEnumerable<WorkoutSessionLog> logs = await _memberWorkoutRepository.GetWorkoutLogsAsync(userId);
            List<(WorkoutSessionLog Log, LoggedExercise Exercise)> tracked = GetTrackedSessions(logs);

            Dictionary<string, string> categoryMap = await _workoutRepository.GetCategoriesForNamesAsync(
                tracked.Select(x => NormalizeName(x.Exercise.Name)).Distinct());

            DateTime weekStart = DateTime.UtcNow.Date.AddDays(-(WeeklyWindowDays - 1));
            DateTime previousWeekStart = weekStart.AddDays(-WeeklyWindowDays);

            return [.. tracked
                .GroupBy(x => ResolveMuscleGroup(x.Exercise.Name, categoryMap) ?? UncategorizedGroup)
                .Select(group =>
                {
                    List<(WorkoutSessionLog Log, LoggedExercise Exercise)> thisWeek = [.. group.Where(x => x.Log.Date.Date >= weekStart)];
                    List<(WorkoutSessionLog Log, LoggedExercise Exercise)> lastWeek = [.. group.Where(x => x.Log.Date.Date >= previousWeekStart && x.Log.Date.Date < weekStart)];

                    return new MuscleGroupProgressDto
                    {
                        MuscleGroup = group.Key,
                        WeeklySessions = thisWeek.Select(x => x.Log.Id).Distinct().Count(),
                        WeeklySets = thisWeek.Sum(x => x.Exercise.LoggedSets.Count(s => s.Completed)),
                        WeeklyVolume = thisWeek.Sum(x => Volume(x.Exercise)),
                        PreviousWeeklyVolume = lastWeek.Sum(x => Volume(x.Exercise)),
                        Exercises = [.. group
                            .GroupBy(x => NormalizeName(x.Exercise.Name))
                            .Select(ToGroupExercise)
                            .OrderByDescending(e => e.Last.Date)]
                    };
                })
                .OrderByDescending(g => g.Exercises.Max(e => e.Last.Date))];
        }

        private static MuscleGroupExerciseDto ToGroupExercise(IEnumerable<(WorkoutSessionLog Log, LoggedExercise Exercise)> sessions)
        {
            List<ExerciseProgressPointDto> points = [.. sessions
                .Select(x => ToPoint(x.Log, x.Exercise))
                .OrderBy(p => p.Date)];

            ExerciseProgressPointDto last = points[^1];
            ExerciseProgressPointDto? previous = points.Count > 1 ? points[^2] : null;
            bool isBodyweight = points.All(p => p.TopWeight <= 0);

            return new MuscleGroupExerciseDto
            {
                Name = sessions.OrderByDescending(x => x.Log.Date).First().Exercise.Name.Trim(),
                TotalSessions = points.Count,
                Last = last,
                Previous = previous,
                IsNewPersonalBest = previous != null && !isBodyweight && last.TopWeight > points.Take(points.Count - 1).Max(p => p.TopWeight),
                Trend = [.. points.TakeLast(TrendLength).Select(p => isBodyweight ? p.TotalReps : p.TopWeight)]
            };
        }

        private static List<(WorkoutSessionLog Log, LoggedExercise Exercise)> GetTrackedSessions(IEnumerable<WorkoutSessionLog> logs)
        {
            return [.. logs
                .SelectMany(l => l.LoggedExercises.Select(e => (Log: l, Exercise: e)))
                .Where(x => !x.Exercise.Skipped && !x.Exercise.IsCardio && x.Exercise.LoggedSets.Any(s => s.Completed))];
        }

        private static ExerciseProgressPointDto ToPoint(WorkoutSessionLog log, LoggedExercise exercise)
        {
            List<LoggedSet> completedSets = [.. exercise.LoggedSets.Where(s => s.Completed).OrderBy(s => s.SetNo)];
            LoggedSet topSet = completedSets.OrderByDescending(s => s.Weight).ThenByDescending(s => s.Reps).First();

            return new ExerciseProgressPointDto
            {
                SessionLogId = log.Id,
                Date = log.Date,
                DayName = log.DayName,
                TopWeight = topSet.Weight,
                TopWeightReps = topSet.Reps,
                TotalSets = completedSets.Count,
                TotalReps = completedSets.Sum(s => s.Reps),
                Volume = Math.Round(completedSets.Sum(s => s.Weight * s.Reps), 1),
                Sets = [.. completedSets.Select((s, i) => new ProgressSetDto { SetNo = i + 1, Weight = s.Weight, Reps = s.Reps })]
            };
        }

        private static double Volume(LoggedExercise exercise) =>
            exercise.LoggedSets.Where(s => s.Completed).Sum(s => s.Weight * s.Reps);

        private static string NormalizeName(string name) => name.Trim().ToLower();

        private static string? ResolveMuscleGroup(string exerciseName, Dictionary<string, string> categoryMap) =>
            categoryMap.TryGetValue(NormalizeName(exerciseName), out string? category) && !string.IsNullOrWhiteSpace(category)
                ? category.Trim()
                : ExerciseMuscleGroupClassifier.Classify(exerciseName);
    }
}
