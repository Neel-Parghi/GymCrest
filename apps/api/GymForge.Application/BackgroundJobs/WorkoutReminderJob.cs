using GymForge.Application.Modules.Workout.Helpers;
using GymForge.Application.Modules.Workout.Interface;
using GymForge.Contracts.WorkoutPlan;
using GymForge.Domain.Entities;
using GymForge.Domain.Interface;
using GymForge.Shared.Helpers;
using Microsoft.Extensions.Logging;

namespace GymForge.Application.BackgroundJobs
{
    /// <summary>
    /// Daily job: emails members who have a non-rest-day workout scheduled today and haven't
    /// logged it yet, provided they haven't opted out via Email Notifications / Workout Reminders
    /// in Account Settings. Only members with a linked User account (and therefore a UserPreference
    /// row to check) are considered - a GymMember with no linked account has never had the chance
    /// to opt in or out, so they're skipped rather than emailed unconditionally.
    /// </summary>
    public class WorkoutReminderJob
    {
        private readonly IMemberWorkoutRepository _memberWorkoutRepository;
        private readonly IMemberWorkoutService _memberWorkoutService;
        private readonly IEmailService _emailService;
        private readonly ILogger<WorkoutReminderJob> _logger;

        public WorkoutReminderJob(
            IMemberWorkoutRepository memberWorkoutRepository,
            IMemberWorkoutService memberWorkoutService,
            IEmailService emailService,
            ILogger<WorkoutReminderJob> logger)
        {
            _memberWorkoutRepository = memberWorkoutRepository;
            _memberWorkoutService = memberWorkoutService;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task ExecuteAsync()
        {
            _logger.LogInformation("WorkoutReminderJob started.");

            IEnumerable<MemberPlanAssignment> assignments = await _memberWorkoutRepository.GetAllActiveAssignmentsAsync();
            // The job runs at 04:30 IST (still the previous day in UTC), so "today" must be the IST date.
            DateTime today = AppTimeZone.Today;
            (DateTime dayStartUtc, DateTime dayEndUtc) = AppTimeZone.DayBoundsUtc(today);
            int sentCount = 0;

            foreach (MemberPlanAssignment assignment in assignments)
            {
                try
                {
                    User? user = assignment.Member?.User ?? assignment.User;
                    if (user == null || string.IsNullOrWhiteSpace(user.Email))
                    {
                        continue; // No linked account / no email on file - never had a chance to opt in or out.
                    }

                    bool emailEnabled = user.Preference?.EmailNotificationsEnabled ?? true;
                    bool remindersEnabled = user.Preference?.WorkoutRemindersEnabled ?? true;
                    if (!emailEnabled || !remindersEnabled)
                    {
                        continue;
                    }

                    Guid targetId = assignment.MemberId ?? assignment.UserId!.Value;

                    WorkoutPlanDto? plan = await _memberWorkoutService.GetActivePlanForMemberAsync(targetId);
                    WorkoutPlanDayDto? todayPlan = WorkoutScheduleResolver.ResolveScheduledDay(plan?.Days, today);

                    if (todayPlan == null || todayPlan.IsRestDay)
                    {
                        continue;
                    }

                    IEnumerable<WorkoutSessionLog> todaysLogs = await _memberWorkoutRepository.GetLogsInRangeAsync(targetId, dayStartUtc, dayEndUtc);
                    if (todaysLogs.Any())
                    {
                        continue; // Already logged - no need to remind.
                    }

                    string firstName = assignment.Member?.FirstName ?? user.FirstName;
                    string subject = "Today's workout is waiting for you";
                    string body = $"<p>Hi {firstName},</p>" +
                                   $"<p>You've got <strong>{todayPlan.DayName}</strong> ({todayPlan.Category}) scheduled today. " +
                                   "Log in to Elevatyon and get it done!</p>" +
                                   "<p style=\"color:#94a3b8;font-size:12px;\">You're receiving this because Workout Reminders are enabled in your Account Settings. " +
                                   "You can turn them off any time from there.</p>";

                    await _emailService.SendEmailAsync(user.Email, firstName, subject, body);
                    sentCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process workout reminder for assignment {AssignmentId}.", assignment.Id);
                }
            }

            _logger.LogInformation("WorkoutReminderJob completed. Sent {SentCount} reminder(s).", sentCount);
        }
    }
}
