using GymCrest.Domain.Entities;
using GymCrest.Domain.Interface;
using GymCrest.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using GymCrest.Infrastructure.Extensions;
using GymCrest.Shared.Models;
using GymCrest.Contracts.Common;

namespace GymCrest.Infrastructure.Repositories
{
    public class StaffRepository : IStaffRepository
    {
        private readonly AppDbContext _dbContext;

        public StaffRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddAsync(Staff staff)
        {
            await _dbContext.Staff.AddAsync(staff);
        }

        public async Task<Staff?> GetByIdAsync(Guid id)
        {
            return await _dbContext.Staff
                .Include(x => x.PTAssignments)
                .ThenInclude(a => a.Member)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<Staff?> GetByUserIdAsync(Guid userId)
        {
            return await _dbContext.Staff
                .Include(x => x.PTAssignments)
                .ThenInclude(a => a.Member)
                .FirstOrDefaultAsync(x => x.UserId == userId);
        }

        public async Task<IEnumerable<Staff>> GetAllByGymIdAsync(Guid gymId, Guid? branchId = null)
        {
            IQueryable<Staff> query = _dbContext.Staff
                .AsNoTracking()
                .Where(x => x.GymId == gymId);

            query = query.WhereBranchContext(_dbContext.Branches, branchId);

            return await query.OrderBy(x => x.LastName).ToListAsync();
        }

        public async Task<(IEnumerable<Staff> Items, int TotalCount)> GetPagedStaffAsync(Guid gymId, int pageNumber, int pageSize, string? searchTerm, Guid? branchId = null)
        {
            IQueryable<Staff> query = _dbContext.Staff
                .AsNoTracking()
                .Where(x => x.GymId == gymId);

            query = query.WhereBranchContext(_dbContext.Branches, branchId);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                query = query.Where(x => x.FirstName.ToLower().Contains(searchTerm) ||
                                       x.LastName.ToLower().Contains(searchTerm) ||
                                       x.Email.ToLower().Contains(searchTerm));
            }

            int totalCount = await query.CountAsync();

            List<Staff> items = await query
                .OrderBy(x => x.LastName)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public Task UpdateAsync(Staff staff)
        {
            _dbContext.Staff.Update(staff);
            return Task.CompletedTask;
        }

        public async Task DeleteAsync(Guid id)
        {
            Staff? staff = await _dbContext.Staff.FindAsync(id);
            if (staff != null)
            {
                _dbContext.Staff.Remove(staff);
            }
        }

        public Task<bool> ExistsByEmailAsync(string email, Guid gymId)
        {
            return _dbContext.Staff.AnyAsync(x => x.Email == email && x.GymId == gymId);
        }

        public async Task AddPTAssignmentAsync(PTAssignment assignment)
        {
            await _dbContext.PTAssignments.AddAsync(assignment);
        }

        public async Task<IEnumerable<PTAssignment>> GetAssignmentsByTrainerIdAsync(Guid trainerId)
        {
            return await _dbContext.PTAssignments
                .Include(x => x.Member)
                .Where(x => x.TrainerId == trainerId)
                .OrderByDescending(x => x.StartDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<PTAssignment>> GetAssignmentsByMemberIdAsync(Guid memberId)
        {
            return await _dbContext.PTAssignments
                .Include(x => x.Trainer)
                .Where(x => x.MemberId == memberId)
                .OrderByDescending(x => x.StartDate)
                .ToListAsync();
        }

        public async Task<PTAssignment?> GetActiveAssignmentAsync(Guid trainerId, Guid memberId)
        {
            return await _dbContext.PTAssignments
                .FirstOrDefaultAsync(x => x.TrainerId == trainerId && x.MemberId == memberId && x.IsActive && (x.EndDate == null || x.EndDate > DateTime.UtcNow));
        }

        public async Task AddMeasurementAsync(MemberMeasurement measurement)
        {
            await _dbContext.MemberMeasurements.AddAsync(measurement);
        }

        public async Task<IEnumerable<MemberMeasurement>> GetMeasurementsByMemberIdAsync(Guid memberOrUserId)
        {
            return await _dbContext.MemberMeasurements
                .AsNoTracking()
                .Include(x => x.RecordedBy)
                .Where(x => x.MemberId == memberOrUserId || x.UserId == memberOrUserId)
                .OrderByDescending(x => x.Date)
                .ToListAsync();
        }

        public async Task AddStaffAttendanceLogAsync(StaffAttendanceLog log)
        {
            await _dbContext.StaffAttendanceLogs.AddAsync(log);
        }

        public async Task<StaffAttendanceLog?> GetActiveStaffAttendanceLogAsync(Guid staffId)
        {
            return await _dbContext.StaffAttendanceLogs
                .FirstOrDefaultAsync(x => x.StaffId == staffId && x.CheckOutTime == null);
        }

        public async Task<IEnumerable<StaffAttendanceLog>> GetStaffAttendanceLogsAsync(Guid gymId, Guid? branchId = null, Guid? staffId = null)
        {
            IQueryable<StaffAttendanceLog> query = _dbContext.StaffAttendanceLogs
                .AsNoTracking()
                .Include(x => x.Staff)
                .Where(x => x.GymId == gymId);

            if (branchId != null)
            {
                query = query.Where(x => x.BranchId == branchId);
            }

            if (staffId != null)
            {
                query = query.Where(x => x.StaffId == staffId);
            }

            return await query
                .OrderByDescending(x => x.CheckInTime)
                .ToListAsync();
        }

        public async Task<PagedResponse<StaffAttendanceLog>> GetStaffAttendanceLogsPagedAsync(
            Guid gymId,
            PaginationParams pagination,
            Guid? branchId = null,
            Guid? staffId = null)
        {
            IQueryable<StaffAttendanceLog> query = _dbContext.StaffAttendanceLogs
                .AsNoTracking()
                .Include(x => x.Staff)
                .Where(x => x.GymId == gymId);

            if (branchId != null)
            {
                query = query.Where(x => x.BranchId == branchId);
            }

            if (staffId != null)
            {
                query = query.Where(x => x.StaffId == staffId);
            }

            if (!string.IsNullOrEmpty(pagination.SearchTerm))
            {
                string term = pagination.SearchTerm.ToLower();
                query = query.Where(x => 
                    (x.Staff.FirstName + " " + x.Staff.LastName).ToLower().Contains(term) ||
                    x.Staff.StaffNumber.ToLower().Contains(term));
            }

            int totalCount = await query.CountAsync();

            List<StaffAttendanceLog> items = await query
                .OrderByDescending(x => x.CheckInTime)
                .Skip((pagination.PageNumber - 1) * pagination.PageSize)
                .Take(pagination.PageSize)
                .ToListAsync();

            return new PagedResponse<StaffAttendanceLog>(items, totalCount, pagination.PageNumber, pagination.PageSize);
        }
    }
}
