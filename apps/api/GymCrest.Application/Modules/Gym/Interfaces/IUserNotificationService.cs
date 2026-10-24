using GymCrest.Contracts.Notifications;

namespace GymCrest.Application.Modules.Gym.Interfaces
{
    public interface IUserNotificationService
    {
        Task<List<UserNotificationResponse>> GetMyNotificationsAsync(Guid userId);
       
        Task MarkAsReadAsync(Guid notificationId, Guid userId);
        
        Task MarkAllAsReadAsync(Guid userId);
        
        Task SendNotificationFromTemplateAsync(Guid userId, Guid gymId, Guid? branchId, int templateType, Dictionary<string, string> variables);
    }
}
