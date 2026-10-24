using GymCrest.Contracts.Gym.Inventory;
using GymCrest.Contracts.Common;
using GymCrest.Shared.Models;

namespace GymCrest.Application.Modules.Gym.Interfaces
{
    public interface IMaintenanceService
    {
        Task<bool> LogMaintenanceAsync(LogMaintenanceDto dto);
        Task<List<MaintenanceLogDto>> GetMaintenanceHistoryAsync(Guid equipmentId);
        Task<PagedResponse<MaintenanceLogDto>> GetAllMaintenanceLogsAsync(Guid gymId, PaginationParams pagination, Guid? branchId = null);
    }
}
