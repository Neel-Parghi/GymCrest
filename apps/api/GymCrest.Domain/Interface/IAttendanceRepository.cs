using GymCrest.Domain.Entities;

namespace GymCrest.Domain.Interface
{
    public interface IAttendanceRepository
    {
        Task AddAsync(AttendanceLog log);
        Task<AttendanceLog?> GetActiveCheckInAsync(Guid memberId, bool includeDetails = true);
        Task<IEnumerable<AttendanceLog>> GetActiveOccupancyAsync(Guid gymId, Guid? branchId);
        Task<int> GetActiveOccupancyCountAsync(Guid gymId, Guid? branchId);
        Task<(IEnumerable<AttendanceLog> Items, int TotalCount)> GetLogsPagedAsync(Guid gymId, Guid? branchId, string? search, string? status, DateTime? date, int pageNumber, int pageSize);
        Task UpdateAsync(AttendanceLog log);
    }
}
