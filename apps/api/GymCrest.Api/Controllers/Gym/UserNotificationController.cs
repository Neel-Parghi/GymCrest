using GymCrest.Application.Modules.Gym.Interfaces;
using GymCrest.Contracts.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymCrest.Api.Controllers.Gym
{
    [ApiController]
    [Route("api/notifications")]
    [Authorize]
    public class UserNotificationController : BaseApiController
    {
        private readonly IUserNotificationService _notificationService;

        public UserNotificationController(IUserNotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpGet]
        public async Task<IActionResult> GetMyNotifications()
        {
            List<UserNotificationResponse> result = await _notificationService.GetMyNotificationsAsync(UserId);
            return Ok(result);
        }

        [HttpPost("{id}/read")]
        public async Task<IActionResult> MarkAsRead(Guid id)
        {
            await _notificationService.MarkAsReadAsync(id, UserId);
            return Ok();
        }

        [HttpPost("read-all")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            await _notificationService.MarkAllAsReadAsync(UserId);
            return Ok();
        }
    }
}
