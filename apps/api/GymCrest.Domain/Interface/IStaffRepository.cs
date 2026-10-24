using GymCrest.Domain.Entities;
using GymCrest.Shared.Models;
using GymCrest.Contracts.Common;

namespace GymCrest.Domain.Interface
{
    public interface IStaffRepository
    {
        Task AddAsync(Staff staff);
        Task<Staff?> GetByIdAsync(Guid id);
        Task<Staff?> GetByUserIdAsync(Guid userId);
        Task<IEnumerable<Staff>> GetAllByGymIdAsync(Guid gymId, Guid? branchId = null);
        Task<(IEnumerable<Staff> Items, int TotalCount)> GetPagedStaffAsync(Guid gymId, int pageNumber, int pageSize, string? searchTerm, Guid? branchId = null);
        Task UpdateAsync(Staff staff);
        Task DeleteAsync(Guid id);
        Task<bool> ExistsByEmailAsync(string email, Guid gymId);
        
        // PT Assignments
        Task AddPTAssignmentAsync(PTAssignment assignment);
        Task<IEnumerable<PTAssignment>> GetAssignmentsByTrainerIdAsync(Guid trainerId);
        Task<IEnumerable<PTAssignment>> GetAssignmentsByMemberIdAsync(Guid memberId);
        Task<PTAssignment?> GetActiveAssignmentAsync(Guid trainerId, Guid memberId);
        
        // Measurements
        Task AddMeasurementAsync(MemberMeasurement measurement);
        Task<IEnumerable<MemberMeasurement>> GetMeasurementsByMemberIdAsync(Guid memberOrUserId);

        // Staff Attendance
        Task AddStaffAttendanceLogAsync(StaffAttendanceLog log);
        Task<StaffAttendanceLog?> GetActiveStaffAttendanceLogAsync(Guid staffId);
        Task<IEnumerable<StaffAttendanceLog>> GetStaffAttendanceLogsAsync(Guid gymId, Guid? branchId = null, Guid? staffId = null);
        Task<PagedResponse<StaffAttendanceLog>> GetStaffAttendanceLogsPagedAsync(Guid gymId, PaginationParams pagination, Guid? branchId = null, Guid? staffId = null);
    }
}
