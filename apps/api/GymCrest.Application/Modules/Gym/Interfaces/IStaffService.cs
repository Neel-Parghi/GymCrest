using GymCrest.Contracts.Common;
using GymCrest.Contracts.Staff;
using GymCrest.Shared.Models;

namespace GymCrest.Application.Modules.Gym.Interfaces
{
    public interface IStaffService
    {
        Task<StaffResponse> AddStaffAsync(Guid gymId, AddStaffRequest request);
        Task<PagedResponse<StaffResponse>> GetGymStaffAsync(Guid gymId, PaginationParams pagination, Guid? branchId = null);
        Task<StaffResponse?> GetStaffByIdAsync(Guid id);
        Task UpdateStaffAsync(Guid id, AddStaffRequest request);
        Task DeleteStaffAsync(Guid id);
        
        // PT Assignments
        Task AssignTrainerToMemberAsync(Guid trainerId, Guid memberId, string? slot = null, int? durationDays = null);
        Task<IEnumerable<TrainerMemberResponse>> GetAssignedMembersAsync(Guid trainerId);
        Task DeallocateMemberFromTrainerAsync(Guid trainerId, Guid memberId);
        
        // Measurements
        Task RecordMeasurementAsync(Guid memberId, Guid recordedById, AddMeasurementRequest request, Guid? gymId);
        Task<IEnumerable<MeasurementResponse>> GetMemberMeasurementsAsync(Guid memberId);

        // Trainer Shift / Presence
        Task<StaffResponse> CheckInStaffAsync(Guid staffId, Guid gymId, Guid? branchId, string? notes);
        Task<StaffResponse> CheckOutStaffAsync(Guid staffId, Guid gymId, Guid? branchId);
        Task<IEnumerable<StaffAttendanceLogResponse>> GetStaffAttendanceLogsAsync(Guid gymId, Guid? branchId = null, Guid? staffId = null);
        Task<PagedResponse<StaffAttendanceLogResponse>> GetStaffAttendanceLogsPagedAsync(Guid gymId, PaginationParams pagination, Guid? branchId = null, Guid? staffId = null);
    }
}
