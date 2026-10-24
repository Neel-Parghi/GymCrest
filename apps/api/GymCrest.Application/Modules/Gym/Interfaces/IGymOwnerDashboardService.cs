using GymCrest.Contracts.Gym.Dashboard;

namespace GymCrest.Application.Modules.Gym.Interfaces
{
    public interface IGymOwnerDashboardService
    {
        Task<GymOwnerDashboardDto> GetGymOwnerDashboardStatsAsync(Guid gymId, Guid? branchId = null);
    }
}
