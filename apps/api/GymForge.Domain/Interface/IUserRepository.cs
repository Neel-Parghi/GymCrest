using GymForge.Domain.Entities;
using GymForge.Contracts.Users;

namespace GymForge.Domain.Interface
{
    public interface IUserRepository
    {
        Task<User?> GetUserByIdAsync(Guid userId);
        
        Task<Guid?> GetBranchIdByUserIdAsync(Guid userId);
        
        Task LinkUserToGymMembersAsync(User user);
        
        Task DeleteUserAsync(User user);
        
        Task<IEnumerable<User>> GetPendingDeletionRequestsAsync();
        
        Task<(IEnumerable<User> Items, int TotalCount)> GetStandaloneUsersAsync(int pageNumber, int pageSize, string? searchTerm);
        
        Task UpdateUserAsync(User user);
        
        Task<UserDashboardSummaryDto> GetUserDashboardSummaryAsync(Guid userId);

        Task<(double? HeightCm, double? WeightKg)> GetLatestBodyMetricsAsync(Guid userId);
        
        Task<IEnumerable<DailyRoutine>> GetDailyRoutinesAsync(Guid userId);
        
        Task<DailyRoutine?> GetDailyRoutineByIdAsync(Guid id);
        
        Task AddDailyRoutineAsync(DailyRoutine routine);
        
        Task UpdateDailyRoutineAsync(DailyRoutine routine);
        
        Task<DailyRoutineCompletion?> GetDailyRoutineCompletionAsync(Guid userId, DateTime date);
        
        Task AddDailyRoutineCompletionAsync(DailyRoutineCompletion completion);
        
        Task UpdateDailyRoutineCompletionAsync(DailyRoutineCompletion completion);
    }
}
