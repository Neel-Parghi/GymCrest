using GymCrest.Application.Modules.Workout.Helpers;
using GymCrest.Contracts.Users;
using GymCrest.Domain.Entities;
using GymCrest.Domain.Interface;
using GymCrest.Infrastructure.Persistence;
using GymCrest.Shared.Constants;
using GymCrest.Shared.Enums;
using GymCrest.Shared.Helpers;
using Microsoft.EntityFrameworkCore;

namespace GymCrest.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _dbContext;

        private const double StrengthMinutesPerSet = 3.0;     
        private const double CardioFallbackMinutes = 10.0;
        private const double CardioMaxMinutesPerSet = 180.0;  
        private const double StrengthMet = 5.0;               
        private const double CardioMet = 7.0;                 
        private const double DefaultBodyWeightKg = 70.0;

        // Muscle recovery window constants
        private const double DefaultRecoveryHours = 48;
        private static readonly Dictionary<string, double> MuscleRecoveryHours = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Back"] = 72,
            ["Chest"] = 72,
            ["Legs"] = 72,
            ["Shoulders"] = 60,
            ["Biceps"] = 36,
            ["Triceps"] = 36,
            ["Core"] = 36,
            ["Cardio"] = 24,
        };
        private const int RecoveryLookbackDays = 30;

        private const int StreakLookbackDays = 400;
        private const int DefaultMonthlySessionTarget = 20;
        private const double WeightGoalToleranceKg = 0.5;
        private const double WeeksPerMonth = 52.0 / 12.0;

        public UserRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<User?> GetUserByIdAsync(Guid userId)
        {
            return await _dbContext.Users
                .Include(u => u.Profile)
                    .ThenInclude(p => p.Address)
                .Include(u => u.Preference)
                .Include(u => u.Security)
                .FirstOrDefaultAsync(u => u.Id == userId);
        }

        public async Task<Guid?> GetBranchIdByUserIdAsync(Guid userId)
        {
            return await _dbContext.Staff
                .Where(s => s.UserId == userId && s.IsActive)
                .Select(s => s.BranchId)
                .FirstOrDefaultAsync();
        }

        public async Task LinkUserToGymMembersAsync(User user)
        {
            string emailLower = user.Email.ToLower();
            List<GymMember> unlinkedMembers = await _dbContext.GymMembers
                .Where(m => m.Email.ToLower() == emailLower && m.UserId == null)
                .ToListAsync();

            foreach (GymMember member in unlinkedMembers)
            {
                member.UserId = user.Id;
                if (user.GymId == null) 
                {
                    user.GymId = member.GymId;
                }
            }
        }

        public async Task DeleteUserAsync(User user)
        {
            await _dbContext.WorkoutPlans
                .Where(p => p.CreatedBy == user.Id && p.IsCustom)
                .ExecuteDeleteAsync();

            await _dbContext.DietPlans
                .Where(p => p.CreatedBy == user.Id && p.IsCustom)
                .ExecuteDeleteAsync();

            _dbContext.Users.Remove(user);
        }

        public async Task<IEnumerable<User>> GetPendingDeletionRequestsAsync()
        {
            return await _dbContext.Users
                .Include(u => u.Security)
                .Where(u => u.Security != null && 
                            u.Security.DeletionRequestedOn != null && 
                            u.Role != UserRole.GymOwner && 
                            u.GymId == null)
                .ToListAsync();
        }

        public async Task<(IEnumerable<User> Items, int TotalCount)> GetStandaloneUsersAsync(int pageNumber, int pageSize, string? searchTerm)
        {
            IQueryable<User> query = _dbContext.Users
                .Include(u => u.Profile)
                .Include(u => u.Preference)
                .Include(u => u.Security)
                .Where(u => u.Role != UserRole.GymOwner && u.GymId == null);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                string searchLower = searchTerm.ToLower();
                query = query.Where(u => 
                    u.FirstName.ToLower().Contains(searchLower) || 
                    u.LastName.ToLower().Contains(searchLower) || 
                    u.Email.ToLower().Contains(searchLower));
            }

            int totalCount = await query.CountAsync();

            List<User> items = await query
                .OrderByDescending(u => u.CreatedOn)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public Task UpdateUserAsync(User user)
        {
            _dbContext.Users.Update(user);
            return Task.CompletedTask;
        }

        private IQueryable<MemberMeasurement> MeasurementsOf(Guid userId) =>
            _dbContext.MemberMeasurements
                .AsNoTracking()
                .Where(m => m.UserId == userId || (m.Member != null && m.Member.UserId == userId));

        public async Task<(double? HeightCm, double? WeightKg)> GetLatestBodyMetricsAsync(Guid userId)
        {
            double? height = await MeasurementsOf(userId)
                .Where(m => m.Height.HasValue && m.Height > 0)
                .OrderByDescending(m => m.Date)
                .Select(m => m.Height)
                .FirstOrDefaultAsync();

            double? weight = await MeasurementsOf(userId)
                .Where(m => m.Weight.HasValue && m.Weight > 0)
                .OrderByDescending(m => m.Date)
                .Select(m => m.Weight)
                .FirstOrDefaultAsync();

            return (height, weight);
        }

        public async Task<UserDashboardSummaryDto> GetUserDashboardSummaryAsync(Guid userId)
        {
            UserDashboardSummaryDto summary = new UserDashboardSummaryDto();

            User? user = await _dbContext.Users
                .AsNoTracking()
                .Include(u => u.Preference)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) return summary;

            summary.UserName = user.FirstName;
            int istHour = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, AppTimeZone.India).Hour;
            summary.Greeting = istHour < 12 ? "Good morning" : (istHour < 18 ? "Good afternoon" : "Good evening");

            summary.ShowGymBuddy = user.Preference?.ShowGymBuddy ?? true;
            summary.GoalTitle = FitnessGoals.Normalize(user.Preference?.PrimaryGoal);
            summary.TargetCalories = user.Preference?.TargetCalories ?? 2500;
            summary.TargetTrainingTime = user.Preference?.TargetTrainingTime ?? 60;
            summary.TargetWeight = user.Preference?.TargetWeight;

            MemberMeasurement? latestMeasurement = await MeasurementsOf(userId)
                .OrderByDescending(m => m.Date)
                .FirstOrDefaultAsync();

            if (latestMeasurement != null)
            {
                summary.BodyFat = latestMeasurement.BodyFatPercentage ?? 0;
                summary.BMI = latestMeasurement.BMI ?? 0;

                double? latestWeight = await MeasurementsOf(userId)
                    .Where(m => m.Weight.HasValue && m.Weight > 0)
                    .OrderByDescending(m => m.Date)
                    .Select(m => m.Weight)
                    .FirstOrDefaultAsync();

                summary.CurrentWeight = latestWeight ?? 0;

                // Targets saved before the start weight was recorded use the latest weigh-in at their
                // last save, then the first weigh-in.
                double? startWeight = user.Preference?.TargetWeightStart;
                if (!startWeight.HasValue && user.Preference != null)
                {
                    DateTime targetSavedAt = user.Preference.ModifiedOn ?? user.Preference.CreatedOn;
                    startWeight = await MeasurementsOf(userId)
                        .Where(m => m.Weight.HasValue && m.Weight > 0 && m.Date <= targetSavedAt)
                        .OrderByDescending(m => m.Date)
                        .Select(m => m.Weight)
                        .FirstOrDefaultAsync()
                        ?? await MeasurementsOf(userId)
                            .Where(m => m.Weight.HasValue && m.Weight > 0)
                            .OrderBy(m => m.Date)
                            .Select(m => m.Weight)
                            .FirstOrDefaultAsync();
                }

                if (startWeight.HasValue && user.Preference?.TargetWeight.HasValue == true && latestWeight.HasValue)
                {
                    double initialWeight = startWeight.Value;
                    double currentWeight = latestWeight.Value;
                    double targetWeight = user.Preference.TargetWeight.Value;

                    double progress = Math.Abs(initialWeight - targetWeight) < WeightGoalToleranceKg
                        ? (Math.Abs(currentWeight - targetWeight) <= WeightGoalToleranceKg ? 100 : 0)
                        : (targetWeight < initialWeight)
                            ? (initialWeight - currentWeight) / (initialWeight - targetWeight) * 100
                            : (currentWeight - initialWeight) / (targetWeight - initialWeight) * 100;

                    summary.GoalProgressPct = Math.Clamp((int)Math.Round(progress), 0, 100);
                }
            }

            DateTime today = AppTimeZone.Today;
            DateTime startOfMonth = new DateTime(today.Year, today.Month, 1);
            (DateTime monthStartUtc, _) = AppTimeZone.DayBoundsUtc(startOfMonth);
            (DateTime todayStartUtc, DateTime todayEndUtc) = AppTimeZone.DayBoundsUtc(today);

            summary.MonthlySessionCount = await _dbContext.WorkoutSessionLogs
                .CountAsync(l => l.UserId == userId && l.Date >= monthStartUtc && l.Status == "Completed");
            int? trainingDaysPerWeek = user.Preference?.TrainingDaysPerWeek;
            summary.MonthlySessionTarget = trainingDaysPerWeek is > 0
                ? (int)Math.Round(trainingDaysPerWeek.Value * WeeksPerMonth)
                : DefaultMonthlySessionTarget;
            summary.MonthlyCompletionPct = summary.MonthlySessionTarget > 0 ? (summary.MonthlySessionCount * 100) / summary.MonthlySessionTarget : 0;

            var todaySets = await _dbContext.WorkoutSessionLogs
                .Where(l => l.UserId == userId && l.Date >= todayStartUtc && l.Date < todayEndUtc && l.Status == "Completed")
                .SelectMany(l => l.LoggedExercises)
                .Where(e => !e.Skipped)
                .SelectMany(e => e.LoggedSets
                    .Where(s => s.Completed)
                    .Select(s => new { e.IsCardio, s.Reps }))
                .ToListAsync();

            // Cardio sets log their duration in minutes in the Reps field.
            double cardioMinutes = todaySets
                .Where(s => s.IsCardio)
                .Sum(s => s.Reps > 0 ? Math.Min(s.Reps, CardioMaxMinutesPerSet) : CardioFallbackMinutes);
            double strengthMinutes = todaySets.Count(s => !s.IsCardio) * StrengthMinutesPerSet;
            double bodyWeightKg = latestMeasurement?.Weight is double w && w > 0 ? w : DefaultBodyWeightKg;

            summary.ActiveTrainingTimeMinutes = (int)Math.Round(cardioMinutes + strengthMinutes);
            summary.CardioMinutesToday = (int)Math.Round(cardioMinutes);
            summary.StrengthSetsToday = todaySets.Count(s => !s.IsCardio);
            summary.CaloriesBurnedToday = (int)Math.Round(
                bodyWeightKg * ((CardioMet * cardioMinutes) + (StrengthMet * strengthMinutes)) / 60.0);
            summary.TargetCaloriesBurn = (int)Math.Round(bodyWeightKg * StrengthMet * summary.TargetTrainingTime / 60.0);

            (int streak, bool streakAtRisk) = await CalculateWorkoutStreakAsync(userId, user.GymId, today);
            summary.WorkoutStreak = streak;
            summary.StreakAtRisk = streakAtRisk;

            List<DailyRoutine> activeRoutines = await _dbContext.DailyRoutines
                .AsNoTracking()
                .Where(r => r.UserId == userId && r.IsActive)
                .OrderBy(r => r.Order)
                .ToListAsync();

            DateTime routineDay = AppTimeZone.TodayAsUtcDate;
            DailyRoutineCompletion? todayCompletion = await _dbContext.DailyRoutineCompletions
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.UserId == userId && c.Date == routineDay);

            List<Guid> completedIds = todayCompletion?.CompletedRoutineIds ?? new List<Guid>();

            summary.DailyRoutines = activeRoutines.Select(r => new DailyRoutineDto
            {
                Id = r.Id,
                Title = r.Title,
                Time = r.Time,
                Amount = r.Amount,
                Order = r.Order,
                Completed = completedIds.Contains(r.Id)
            }).ToList();

            // 1. Trophy Room (Personal Records) - All-time max weights per exercise
            IQueryable<LoggedSet> completedSets = _dbContext.LoggedSets
                .Where(s => s.Completed && s.Weight > 0
                    && s.LoggedExercise.WorkoutSessionLog.UserId == userId
                    && s.LoggedExercise.WorkoutSessionLog.Status == "Completed");

            var topRecords = await completedSets
                .GroupBy(s => s.LoggedExercise.Name)
                .Select(g => new { Name = g.Key, Weight = g.Max(s => s.Weight) })
                .OrderByDescending(r => r.Weight)
                .Take(3)
                .ToListAsync();

            if (topRecords.Count > 0)
            {
                List<string> recordNames = topRecords.Select(r => r.Name).ToList();
                List<double> recordWeights = topRecords.Select(r => r.Weight).Distinct().ToList();

                var recordSets = await completedSets
                    .Where(s => recordNames.Contains(s.LoggedExercise.Name) && recordWeights.Contains(s.Weight))
                    .Select(s => new { s.LoggedExercise.Name, s.Weight, s.LoggedExercise.WorkoutSessionLog.Date })
                    .ToListAsync();

                summary.PersonalRecords = topRecords.Select(r => new PersonalRecordDto
                {
                    Name = r.Name,
                    Weight = r.Weight + " kg",
                    Date = recordSets
                        .Where(s => s.Name == r.Name && s.Weight == r.Weight)
                        .Select(s => s.Date)
                        .DefaultIfEmpty()
                        .Min()
                        .ToString("MMM yyyy")
                }).ToList();
            }

            // 2. Muscle Recovery & Activation Heatmap
            (DateTime sevenDaysAgo, _) = AppTimeZone.DayBoundsUtc(today.AddDays(-7));
            (DateTime recoveryLookbackStart, _) = AppTimeZone.DayBoundsUtc(today.AddDays(-RecoveryLookbackDays));

            var recentExercises = await _dbContext.WorkoutSessionLogs
                .Where(l => l.UserId == userId && l.Date >= recoveryLookbackStart && l.Status == "Completed")
                .SelectMany(l => l.LoggedExercises)
                .Where(e => !e.Skipped)
                .Select(e => new {
                    ExerciseName = e.Name,
                    Date = e.WorkoutSessionLog.Date,
                    SetCount = e.LoggedSets.Count(s => s.Completed)
                })
                .ToListAsync();

            static string NormalizeExerciseName(string name) =>
                string.IsNullOrWhiteSpace(name) ? string.Empty : name.Trim().ToLowerInvariant();

            List<string> recentNames = recentExercises
                .Select(e => NormalizeExerciseName(e.ExerciseName))
                .Where(n => n.Length > 0)
                .Distinct()
                .ToList();

            var masterExercises = recentNames.Count == 0
                ? []
                : await _dbContext.Exercises
                    .Where(e => recentNames.Contains(e.Name.Trim().ToLower()))
                    .Select(e => new { e.Name, e.Category })
                    .ToListAsync();

            Dictionary<string, string> categoryMap = masterExercises
                .GroupBy(e => NormalizeExerciseName(e.Name))
                .ToDictionary(g => g.Key, g => g.First().Category);

            var recentByCategory = recentExercises
                .Select(e => new {
                    Category = categoryMap.GetValueOrDefault(NormalizeExerciseName(e.ExerciseName)),
                    e.Date,
                    e.SetCount
                })
                .Where(e => !string.IsNullOrEmpty(e.Category))
                .GroupBy(e => e.Category)
                .ToList();

            foreach (var group in recentByCategory)
            {
                string category = group.Key!;
                DateTime latestDate = group.Max(e => e.Date);
                double hoursSinceTrained = (DateTime.UtcNow - latestDate).TotalHours;

                // Muscle Recovery Logic: larger muscle groups need longer to recover than
                // smaller ones, instead of one flat window for every group.
                double recoveryWindowHours = MuscleRecoveryHours.GetValueOrDefault(category, DefaultRecoveryHours);
                string recoveryStatus = hoursSinceTrained < recoveryWindowHours ? "Recovering" : "Ready";
                int recoveryPct = hoursSinceTrained < recoveryWindowHours
                    ? (int)Math.Clamp((hoursSinceTrained / recoveryWindowHours) * 100, 0, 100)
                    : 100;

                summary.MuscleRecovery.Add(new MuscleRecoveryDto
                {
                    Name = category,
                    Status = recoveryStatus,
                    Pct = recoveryPct
                });

                // Activation Heatmap Logic: volume is still scoped to the last 7 days only
                int totalSetsLast7Days = group.Where(e => e.Date >= sevenDaysAgo).Sum(e => e.SetCount);
                if (totalSetsLast7Days <= 0) continue;

                string level;
                string label;
                if (totalSetsLast7Days >= 10)
                {
                    level = "High";
                    label = "High Volume";
                }
                else if (totalSetsLast7Days >= 5)
                {
                    level = "Medium";
                    label = "Moderate";
                }
                else
                {
                    level = "Low";
                    label = "Light";
                }

                summary.MuscleHeatmap.Add(new MuscleHeatmapDto
                {
                    Name = category,
                    Level = level,
                    Label = label
                });
            }

            // 3. Active Plans
            summary.ActiveWorkoutPlan = await _dbContext.MemberPlanAssignments
                .Where(p => p.UserId == userId && p.IsActive && p.WorkoutPlan != null)
                .Select(p => new ActivePlanSummaryDto
                {
                    PlanId = p.WorkoutPlanId,
                    Name = p.WorkoutPlan.Name,
                    Type = p.WorkoutPlan.Level
                })
                .FirstOrDefaultAsync();

            summary.ActiveDietPlan = await _dbContext.MemberDietAssignments
                .Where(p => p.UserId == userId && p.IsActive && p.DietPlan != null)
                .Select(p => new ActivePlanSummaryDto
                {
                    PlanId = p.DietPlanId,
                    Name = p.DietPlan.Name,
                    Type = p.DietPlan.Goal
                })
                .FirstOrDefaultAsync();

            return summary;
        }

        private enum DayStatus { Completed, RestDay, Missed, Pending, NoSchedule }

        private async Task<(int Streak, bool StreakAtRisk)> CalculateWorkoutStreakAsync(Guid userId, Guid? gymId, DateTime today)
        {
            DateTime lookbackStart = today.AddDays(-StreakLookbackDays);
            (DateTime lookbackStartUtc, _) = AppTimeZone.DayBoundsUtc(lookbackStart);

            var logs = await _dbContext.WorkoutSessionLogs
                .Where(l => (l.UserId == userId || (l.Member != null && l.Member.UserId == userId)) && l.Date >= lookbackStartUtc)
                .Select(l => new { l.Date, l.Status })
                .ToListAsync();

            Dictionary<DateTime, string> logStatusByDate = logs
                .GroupBy(l => AppTimeZone.ToLocalDate(l.Date))
                .ToDictionary(
                    g => g.Key,
                    g => g.Any(l => l.Status == "Completed") ? "Completed" : g.Any(l => l.Status == "RestDay") ? "RestDay" : g.First().Status);

            HashSet<DateTime> holidays = gymId.HasValue
                ? (await _dbContext.GymHolidays
                    .Where(h => h.GymId == gymId.Value && h.Date >= lookbackStartUtc)
                    .Select(h => h.Date)
                    .ToListAsync())
                    .Select(AppTimeZone.ToLocalDate)
                    .ToHashSet()
                : new HashSet<DateTime>();

            List<MemberPlanAssignment> assignments = (await _dbContext.MemberPlanAssignments
                .AsNoTracking()
                .Include(a => a.WorkoutPlan)
                    .ThenInclude(p => p.Days)
                .Include(a => a.CustomScheduleDays)
                .Where(a => a.UserId == userId)
                .ToListAsync())
                .OrderByDescending(a => a.AssignedAt)
                .ToList();

            DayStatus ResolveDayStatus(DateTime date)
            {
                bool isToday = date == today;
                if (logStatusByDate.TryGetValue(date, out string? status))
                {
                    if (status == "Completed") return DayStatus.Completed;
                    if (status == "RestDay") return DayStatus.RestDay;
                }

                if (holidays.Contains(date)) return DayStatus.RestDay;

                MemberPlanAssignment? assignment = assignments.FirstOrDefault(a => AppTimeZone.ToLocalDate(a.AssignedAt) <= date);
                if (assignment?.WorkoutPlan == null) return isToday ? DayStatus.Pending : DayStatus.NoSchedule;

                if (!IsTrainingDay(date, assignment)) return DayStatus.RestDay;
                return isToday ? DayStatus.Pending : DayStatus.Missed;
            }

            DayStatus todayStatus = ResolveDayStatus(today);
            int streak = todayStatus == DayStatus.Completed ? 1 : 0;

            for (DateTime cursor = today.AddDays(-1); cursor > lookbackStart; cursor = cursor.AddDays(-1))
            {
                DayStatus status = ResolveDayStatus(cursor);
                if (status == DayStatus.Completed) streak++;
                else if (status != DayStatus.RestDay) break;
            }

            bool hasPlanToday = assignments.Any(a => AppTimeZone.ToLocalDate(a.AssignedAt) <= today);
            bool streakAtRisk = todayStatus == DayStatus.Pending && hasPlanToday && streak > 0;
            return (streak, streakAtRisk);
        }

        private static bool IsTrainingDay(DateTime date, MemberPlanAssignment assignment)
        {
            string weekday = date.DayOfWeek.ToString();

            MemberWorkoutScheduleDay? overrideDay = assignment.CustomScheduleDays?
                .FirstOrDefault(o => string.Equals(o.DayOfWeek, weekday, StringComparison.OrdinalIgnoreCase));
            if (overrideDay != null) return !overrideDay.IsRestDay;

            WorkoutPlanDay? scheduled = WorkoutScheduleResolver.ResolveScheduledDay(
                assignment.WorkoutPlan.Days, d => d.DayName, d => d.DayIndex, date);
            return scheduled is { IsRestDay: false };
        }

        public async Task<IEnumerable<DailyRoutine>> GetDailyRoutinesAsync(Guid userId)
        {
            return await _dbContext.DailyRoutines.Where(r => r.UserId == userId && r.IsActive).OrderBy(r => r.Order).ToListAsync();
        }

        public async Task<DailyRoutine?> GetDailyRoutineByIdAsync(Guid id)
        {
            return await _dbContext.DailyRoutines.FirstOrDefaultAsync(r => r.Id == id);
        }

        public Task AddDailyRoutineAsync(DailyRoutine routine)
        {
            _dbContext.DailyRoutines.Add(routine);
            return Task.CompletedTask;
        }

        public Task UpdateDailyRoutineAsync(DailyRoutine routine)
        {
            _dbContext.DailyRoutines.Update(routine);
            return Task.CompletedTask;
        }

        public async Task<DailyRoutineCompletion?> GetDailyRoutineCompletionAsync(Guid userId, DateTime date)
        {
            return await _dbContext.DailyRoutineCompletions.FirstOrDefaultAsync(c => c.UserId == userId && c.Date == date.Date);
        }

        public Task AddDailyRoutineCompletionAsync(DailyRoutineCompletion completion)
        {
            _dbContext.DailyRoutineCompletions.Add(completion);
            return Task.CompletedTask;
        }

        public Task UpdateDailyRoutineCompletionAsync(DailyRoutineCompletion completion)
        {
            _dbContext.DailyRoutineCompletions.Update(completion);
            return Task.CompletedTask;
        }
    }
}
