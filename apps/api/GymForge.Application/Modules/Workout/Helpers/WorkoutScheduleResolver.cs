using GymForge.Contracts.WorkoutPlan;

namespace GymForge.Application.Modules.Workout.Helpers
{
    /// <summary>
    /// Picks the plan day scheduled for a date, mirroring the member app (resolveScheduledDay in the web client):
    /// weekly plans match the weekday in the day name; abstract splits ("Day 1", "Day 2"…) fall on
    /// Monday / Wednesday / Friday. Returns null when nothing is scheduled.
    /// </summary>
    public static class WorkoutScheduleResolver
    {
        private static readonly DayOfWeek[] SplitDays = [DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday];

        public static WorkoutPlanDayDto? ResolveScheduledDay(IEnumerable<WorkoutPlanDayDto>? days, DateTime date) =>
            ResolveScheduledDay(days, d => d.DayName, d => d.DayIndex, date);

        /// <inheritdoc cref="WorkoutScheduleResolver"/>
        public static T? ResolveScheduledDay<T>(IEnumerable<T>? days, Func<T, string?> dayName, Func<T, int> dayIndex, DateTime date)
            where T : class
        {
            List<T> ordered = days?.OrderBy(dayIndex).ToList() ?? [];
            if (ordered.Count == 0)
            {
                return null;
            }

            string weekday = date.DayOfWeek.ToString();
            T? weekdayMatch = ordered.FirstOrDefault(d => NameContains(dayName(d), weekday));
            if (weekdayMatch != null)
            {
                return weekdayMatch;
            }

            bool isWeeklyPlan = ordered.Any(d => Enum.GetNames<DayOfWeek>().Any(name => NameContains(dayName(d), name)));
            if (isWeeklyPlan)
            {
                return null;
            }

            int slot = Array.IndexOf(SplitDays, date.DayOfWeek);
            return slot >= 0 && slot < ordered.Count ? ordered[slot] : null;
        }

        private static bool NameContains(string? dayName, string weekday) =>
            dayName?.Contains(weekday, StringComparison.OrdinalIgnoreCase) == true;
    }
}
