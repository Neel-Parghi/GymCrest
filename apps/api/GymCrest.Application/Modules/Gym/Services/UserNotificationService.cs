using GymCrest.Application.Modules.Gym.Interfaces;
using GymCrest.Contracts.Notifications;
using GymCrest.Domain.Entities;
using GymCrest.Domain.Enums;
using GymCrest.Domain.Interface;

namespace GymCrest.Application.Modules.Gym.Services
{
    public class UserNotificationService : IUserNotificationService
    {
        private readonly IUserNotificationRepository _notificationRepository;
        private readonly IAnnouncementTemplateRepository _templateRepository;

        public UserNotificationService(
            IUserNotificationRepository notificationRepository,
            IAnnouncementTemplateRepository templateRepository)
        {
            _notificationRepository = notificationRepository;
            _templateRepository = templateRepository;
        }

        public async Task<List<UserNotificationResponse>> GetMyNotificationsAsync(Guid userId)
        {
            List<UserNotification> notifications = await _notificationRepository.GetByUserIdAsync(userId);
            return notifications.Select(MapToResponse).ToList();
        }

        public async Task MarkAsReadAsync(Guid notificationId, Guid userId)
        {
            UserNotification? notification = await _notificationRepository.GetByIdAsync(notificationId);
            if (notification != null && notification.UserId == userId && !notification.IsRead)
            {
                notification.IsRead = true;
                _notificationRepository.Update(notification);
            }
        }

        public async Task MarkAllAsReadAsync(Guid userId)
        {
            List<UserNotification> notifications = await _notificationRepository.GetByUserIdAsync(userId);
            foreach (UserNotification notification in notifications.Where(n => !n.IsRead))
            {
                notification.IsRead = true;
                _notificationRepository.Update(notification);
            }
        }

        public async Task SendNotificationFromTemplateAsync(Guid userId, Guid gymId, Guid? branchId, int templateType, Dictionary<string, string> variables)
        {
            List<AnnouncementTemplate> templates = await _templateRepository.GetByGymIdAsync(gymId, branchId);
            AnnouncementTemplate? template = templates.FirstOrDefault(t => t.Type == (TemplateType)templateType && t.IsActive);

            if (template != null)
            {
                string title = ProcessTemplate(template.TitleTemplate, variables);
                string message = ProcessTemplate(template.MessageTemplate, variables);

                UserNotification notification = new()
                {
                    GymId = gymId,
                    BranchId = branchId,
                    UserId = userId,
                    Title = title,
                    Message = message,
                    IsRead = false,
                    CreatedBy = Guid.Empty, // System generated
                    CreatedOn = DateTime.UtcNow
                };

                await _notificationRepository.AddAsync(notification);
            }
        }

        private static string ProcessTemplate(string template, Dictionary<string, string> variables)
        {
            if (string.IsNullOrEmpty(template) || variables == null || !variables.Any())
                return template ?? string.Empty;

            string result = template;
            foreach (var kvp in variables)
            {
                result = result.Replace($"{{{{{kvp.Key}}}}}", kvp.Value);
            }
            return result;
        }

        private static UserNotificationResponse MapToResponse(UserNotification entity)
        {
            return new UserNotificationResponse
            {
                Id = entity.Id,
                GymId = entity.GymId,
                BranchId = entity.BranchId,
                UserId = entity.UserId,
                Title = entity.Title,
                Message = entity.Message,
                IsRead = entity.IsRead,
                CreatedOn = entity.CreatedOn
            };
        }
    }
}
