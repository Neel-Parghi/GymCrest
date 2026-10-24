using GymCrest.Domain.Entities;
using GymCrest.Domain.Interface;
using GymCrest.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GymCrest.Infrastructure.Repositories
{
    public class AttendanceRepository : IAttendanceRepository
    {
        private readonly AppDbContext _dbContext;

        public AttendanceRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddAsync(AttendanceLog log)
        {
            await _dbContext.AttendanceLogs.AddAsync(log);
        }

        public async Task<AttendanceLog?> GetActiveCheckInAsync(Guid memberId, bool includeDetails = true)
        {
            IQueryable<AttendanceLog> query = _dbContext.AttendanceLogs;

            if (includeDetails)
            {
                query = query
                    .Include(x => x.Member)
                        .ThenInclude(m => m.Subscriptions)
                    .Include(x => x.Branch);
            }

            return await query.FirstOrDefaultAsync(x => x.MemberId == memberId && x.CheckOutTime == null);
        }

        public async Task<IEnumerable<AttendanceLog>> GetActiveOccupancyAsync(Guid gymId, Guid? branchId)
        {
            IQueryable<AttendanceLog> query = _dbContext.AttendanceLogs
                .AsNoTracking()
                .Include(x => x.Member)
                .ThenInclude(m => m.Subscriptions)
                .Where(x => x.Member.GymId == gymId && x.CheckOutTime == null);

            if (branchId.HasValue)
            {
                query = query.Where(x => x.BranchId == branchId.Value || 
                    (x.BranchId == null && x.Member.BranchId == branchId.Value) ||
                    (x.BranchId == null && x.Member.BranchId == null && _dbContext.Branches.Any(b => b.Id == branchId.Value && b.IsMainBranch)));
            }

            return await query
                .OrderByDescending(x => x.CheckInTime)
                .ToListAsync();
        }

        public async Task<int> GetActiveOccupancyCountAsync(Guid gymId, Guid? branchId)
        {
            IQueryable<AttendanceLog> query = _dbContext.AttendanceLogs
                .Where(x => x.Member.GymId == gymId && x.CheckOutTime == null);

            if (branchId.HasValue)
            {
                query = query.Where(x => x.BranchId == branchId.Value || 
                    (x.BranchId == null && x.Member.BranchId == branchId.Value) ||
                    (x.BranchId == null && x.Member.BranchId == null && _dbContext.Branches.Any(b => b.Id == branchId.Value && b.IsMainBranch)));
            }

            return await query.CountAsync();
        }

        public async Task<(IEnumerable<AttendanceLog> Items, int TotalCount)> GetLogsPagedAsync(Guid gymId, Guid? branchId, string? search, string? status, DateTime? date, int pageNumber, int pageSize)
        {
            IQueryable<AttendanceLog> query = _dbContext.AttendanceLogs
                .AsNoTracking()
                .Where(x => x.Member.GymId == gymId);

            if (branchId.HasValue)
                query = query.Where(x => x.BranchId == branchId.Value || 
                    (x.BranchId == null && x.Member.BranchId == branchId.Value) ||
                    (x.BranchId == null && x.Member.BranchId == null && _dbContext.Branches.Any(b => b.Id == branchId.Value && b.IsMainBranch)));

            if (!string.IsNullOrWhiteSpace(search))
            {
                string s = search.ToLower();
                query = query.Where(x =>
                    x.Member.FirstName.ToLower().Contains(s) ||
                    x.Member.LastName.ToLower().Contains(s) ||
                    x.Member.MembershipNumber.ToLower().Contains(s));
            }

            if (status == "active")
                query = query.Where(x => x.CheckOutTime == null);
            else if (status == "completed")
                query = query.Where(x => x.CheckOutTime != null);

            if (date.HasValue)
            {
                DateTime startOfDay = DateTime.SpecifyKind(date.Value.Date, DateTimeKind.Utc);
                DateTime endOfDay = startOfDay.AddDays(1);
                query = query.Where(x => x.CheckInTime >= startOfDay && x.CheckInTime < endOfDay);
            }

            int total = await query.CountAsync();

            List<AttendanceLog> items = await query
                .Include(x => x.Member)
                    .ThenInclude(m => m.Subscriptions)
                .Include(x => x.Branch)
                .OrderByDescending(x => x.CheckInTime)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, total);
        }

        public Task UpdateAsync(AttendanceLog log)
        {
            _dbContext.AttendanceLogs.Update(log);
            return Task.CompletedTask;
        }
    }
}
