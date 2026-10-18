using GymForge.Contracts.Gym.Management;
using GymForge.Domain.Entities;
using GymForge.Domain.Enums;
using GymForge.Domain.Interface;
using GymForge.Shared.Enums;
using GymForge.Shared.Helpers;

using Microsoft.Extensions.Logging;

namespace GymForge.Application.BackgroundJobs
{
    public class AutomatedNotificationJob
    {
        private readonly IGymManagementRepository _gymRepository;
        private readonly IAnnouncementTemplateRepository _templateRepository;
        private readonly IGymMemberRepository _memberRepository;
        private readonly IUserNotificationRepository _notificationRepository;
        private readonly IEmailService _emailService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AutomatedNotificationJob> _logger;

        public AutomatedNotificationJob(
            IGymManagementRepository gymRepository,
            IAnnouncementTemplateRepository templateRepository,
            IGymMemberRepository memberRepository,
            IUserNotificationRepository notificationRepository,
            IEmailService emailService,
            IUnitOfWork unitOfWork,
            ILogger<AutomatedNotificationJob> logger)
        {
            _gymRepository = gymRepository;
            _templateRepository = templateRepository;
            _memberRepository = memberRepository;
            _notificationRepository = notificationRepository;
            _emailService = emailService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task ExecuteAsync()
        {
            _logger.LogInformation("AutomatedNotificationJob started.");

            // Get all gyms
            List<GymListResponseDto> gyms = await _gymRepository.GetAllGymsAsync();

            foreach (GymListResponseDto gym in gyms)
            {
                await ProcessGymAutomationsAsync(gym.Id, gym.GymName);
            }

            _logger.LogInformation("AutomatedNotificationJob completed.");
        }

        private async Task ProcessGymAutomationsAsync(Guid gymId, string gymName)
        {
            // 1. Fetch templates for this gym
            List<AnnouncementTemplate> templates = await _templateRepository.GetByGymIdAsync(gymId);

            AnnouncementTemplate? expiredTemplate = templates.FirstOrDefault(t => t.Type == TemplateType.ExpiredMembership && t.IsActive);
            
            AnnouncementTemplate? expiringSoonTemplate = templates.FirstOrDefault(t => t.Type == TemplateType.ExpiringSoon && t.IsActive);

            Gym? gym = await _gymRepository.GetGymByIdAsync(gymId);
            int triggerDays = gym?.Configuration?.PlanExpirationTriggerDays ?? 7;

            // 2. Fetch the active subscriptions for this gym (renewal deactivates the previous one, so one per member)
            IEnumerable<MemberSubscription> subscriptions = await _memberRepository.GetActiveSubscriptionsByGymIdAsync(gymId);
            // Runs at 00:00 IST (still the previous day in UTC), so compare IST calendar dates.
            DateTime today = AppTimeZone.Today;

            foreach (var sub in subscriptions)
            {
                // A freeze pushes the end date back only when it is lifted (MemberFreezeCalculator.Unfreeze),
                // so a frozen member's stored end date is not final yet
                if (sub.Member.Status == MemberStatus.Freeze) continue;

                DateTime endDate = AppTimeZone.ToLocalDate(sub.EndDate);

                // EXPIRED: the plan's last day has passed. Always mark it, whether or not the gym sends a message,
                // and catch up on plans that ended on a day the job didn't run (e.g. the host was asleep).
                if (endDate < today)
                {
                    sub.Member.Status = MemberStatus.Expired;
                    sub.IsActive = false;
                    await _memberRepository.UpdateAsync(sub.Member);

                    if (expiredTemplate != null)
                    {
                        await SendNotificationAsync(gymId, gymName, sub, expiredTemplate);
                        await SendEmailAsync(gymId, gymName, sub, expiredTemplate);
                    }
                    continue;
                }

                // EXPIRING SOON
                if (expiringSoonTemplate != null && endDate == today.AddDays(triggerDays))
                {
                    await SendNotificationAsync(gymId, gymName, sub, expiringSoonTemplate);
                    await SendEmailAsync(gymId, gymName, sub, expiringSoonTemplate);
                }
            }

            await _unitOfWork.SaveChangesAsync();
        }

        private async Task SendNotificationAsync(Guid gymId, string gymName, MemberSubscription sub, AnnouncementTemplate template)
        {
            if (sub.Member == null || sub.Member.UserId == null) return;

            string title = ReplaceVariables(template.TitleTemplate, gymName, sub, gymId);
            string message = ReplaceVariables(template.MessageTemplate, gymName, sub, gymId);

            UserNotification notification = new()
            {
                GymId = gymId,
                UserId = sub.Member.UserId.Value,
                Title = title,
                Message = message,
                IsRead = false,
                CreatedOn = DateTime.UtcNow
            };

            await _notificationRepository.AddAsync(notification);
            _logger.LogInformation($"Created notification for User {sub.Member.UserId} (Template: {template.Type}).");
        }

        private async Task SendEmailAsync(Guid gymId, string gymName, MemberSubscription sub, AnnouncementTemplate template)
        {
            if (sub.Member == null || sub.Member.User == null || string.IsNullOrEmpty(sub.Member.User.Email)) return;
            // Membership notices are optional mail: honour the member's "Email notifications" switch (in-app notice still goes out)
            if (!(sub.Member.User.Preference?.EmailNotificationsEnabled ?? true)) return;

            string subject = ReplaceVariables(template.TitleTemplate, gymName, sub, gymId);
            string body = ReplaceVariables(template.MessageTemplate, gymName, sub, gymId);

            try
            {
                await _emailService.SendMembershipNoticeEmailAsync(sub.Member.User.Email, sub.Member.FirstName + " " + sub.Member.LastName, gymName, subject, body);

                _logger.LogInformation($"Sent {template.Type} email to {sub.Member.User.Email}.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send email to {sub.Member.User.Email}");
            }
        }

        private static string ReplaceVariables(string text, string gymName, MemberSubscription sub, Guid gymId)
        {
            if (string.IsNullOrEmpty(text)) return text;

            string userName = sub.Member != null ? sub.Member.FirstName + " " + sub.Member.LastName : "Member";
                       
            string planName = sub.GymPlan?.Name ?? "Membership Plan";
            string expiryDate = sub.EndDate.ToString("MMM dd, yyyy");

            return text
                .Replace("{{UserName}}", userName)
                .Replace("{{GymName}}", gymName)
                .Replace("{{PlanName}}", planName)
                .Replace("{{ExpiryDate}}", expiryDate);
        }
    }
}
