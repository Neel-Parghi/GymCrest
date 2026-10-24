using GymCrest.Contracts.Attendance;
using GymCrest.Contracts.Common;
using GymCrest.Shared.Models;

namespace GymCrest.Application.Modules.Gym.Interfaces
{
    public interface IAttendanceService
    {
        Task<AttendanceLogResponse> CheckInAsync(Guid gymId, Guid? branchId, CheckInRequest request, Guid verifiedByUserId);
        Task<AttendanceLogResponse> CheckOutAsync(Guid gymId, Guid? branchId, CheckOutRequest request, Guid verifiedByUserId);
        Task<IEnumerable<CheckedInMemberResponse>> GetActiveOccupancyAsync(Guid gymId, Guid? branchId);
        Task<OccupancyStatsResponse> GetOccupancyStatsAsync(Guid gymId, Guid? branchId);
        Task<PagedResponse<AttendanceLogResponse>> GetLogsAsync(Guid gymId, Guid? branchId, AttendanceLogQueryParams queryParams);
    }
}
