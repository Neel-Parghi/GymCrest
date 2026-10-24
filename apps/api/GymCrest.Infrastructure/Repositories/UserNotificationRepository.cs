using GymCrest.Domain.Entities;
using GymCrest.Domain.Interface;
using GymCrest.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GymCrest.Infrastructure.Repositories
{
    public class UserNotificationRepository : IUserNotificationRepository
    {
        private readonly AppDbContext _dbContext;

        public UserNotificationRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<UserNotification?> GetByIdAsync(Guid id)
        {
            return await _dbContext.UserNotifications.FirstOrDefaultAsync(n => n.Id == id);
        }

        public async Task<List<UserNotification>> GetByUserIdAsync(Guid userId)
        {
            return await _dbContext.UserNotifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedOn)
                .ToListAsync();
        }

        public async Task AddAsync(UserNotification notification)
        {
            await _dbContext.UserNotifications.AddAsync(notification);
        }

        public void Update(UserNotification notification)
        {
            _dbContext.UserNotifications.Update(notification);
        }

        public async Task DeleteAsync(Guid id)
        {
            UserNotification notification = await GetByIdAsync(id);
            if (notification != null)
            {
                _dbContext.UserNotifications.Remove(notification);
            }
        }
    }
}
