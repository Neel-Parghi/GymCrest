using GymCrest.Domain.Entities;

namespace GymCrest.Domain.Interface
{
    public interface IMaintenanceRepository
    {
        Task AddMaintenanceLogAsync(MaintenanceLog log);
        Task<MaintenanceLog?> GetMaintenanceLogByIdAsync(Guid id);
        void UpdateMaintenanceLog(MaintenanceLog log);
        Task<List<MaintenanceLog>> GetMaintenanceLogsByEquipmentIdAsync(Guid equipmentId);
        Task<List<MaintenanceLog>> GetAllMaintenanceLogsAsync(Guid gymId, Guid? branchId = null);
        Task<(IEnumerable<MaintenanceLog> items, int totalCount)> GetPagedMaintenanceLogsAsync(Guid gymId, int pageNumber, int pageSize, string? searchTerm, Guid? branchId = null);
    }
}
