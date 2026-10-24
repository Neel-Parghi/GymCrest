using GymCrest.Contracts.SuperAdmin.Dashboard;

namespace GymCrest.Application.Modules.SuperAdmin.Interfaces
{
    public interface IDashboardService
    {
        Task<SuperAdminDashboardDto> GetDashboardStatsAsync();
    }
}
