using GymForge.Domain.Entities;
using GymForge.Domain.Interface;
using GymForge.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GymForge.Infrastructure.Repositories
{
    public class MemberWorkoutRepository : IMemberWorkoutRepository
    {
        private readonly AppDbContext _dbContext;

        public MemberWorkoutRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<MemberPlanAssignment?> GetActivePlanAssignmentAsync(Guid memberOrUserId)
        {
            return await _dbContext.MemberPlanAssignments
                .Include(mpa => mpa.CustomScheduleDays)
                .Where(mpa => (mpa.MemberId == memberOrUserId || mpa.UserId == memberOrUserId || (mpa.Member != null && mpa.Member.UserId == memberOrUserId)) && mpa.IsActive)
                .OrderByDescending(mpa => mpa.AssignedAt)
                .FirstOrDefaultAsync();
        }

        public async Task AddPlanAssignmentAsync(MemberPlanAssignment assignment)
        {
            await _dbContext.MemberPlanAssignments.AddAsync(assignment);
        }

        public async Task<IEnumerable<MemberPlanAssignment>> GetAllActiveAssignmentsAsync()
        {
            return await _dbContext.MemberPlanAssignments
                .Include(mpa => mpa.Member!).ThenInclude(m => m.User!).ThenInclude(u => u.Preference)
                .Include(mpa => mpa.User!).ThenInclude(u => u.Preference)
                .Where(mpa => mpa.IsActive)
                .ToListAsync();
        }

        public async Task<IEnumerable<WorkoutSessionLog>> GetWorkoutLogsAsync(Guid memberOrUserId, DateTime? startUtc = null, DateTime? endUtc = null)
        {
            IQueryable<WorkoutSessionLog> query = _dbContext.WorkoutSessionLogs
                .Where(l => l.MemberId == memberOrUserId || l.UserId == memberOrUserId || (l.Member != null && l.Member.UserId == memberOrUserId));

            if (startUtc.HasValue) query = query.Where(l => l.Date >= startUtc.Value);
            if (endUtc.HasValue) query = query.Where(l => l.Date < endUtc.Value);

            return await query
                .Include(l => l.LoggedExercises)
                    .ThenInclude(e => e.LoggedSets)
                .OrderByDescending(l => l.Date)
                .ToListAsync();
        }

        public async Task AddWorkoutSessionLogAsync(WorkoutSessionLog log)
        {
            await _dbContext.WorkoutSessionLogs.AddAsync(log);
        }

        public async Task RemoveWorkoutSessionLogsAsync(IEnumerable<WorkoutSessionLog> logs)
        {
            _dbContext.WorkoutSessionLogs.RemoveRange(logs);
            await Task.CompletedTask;
        }

        public async Task<IEnumerable<WorkoutSessionLog>> GetLogsByDateAsync(Guid memberOrUserId, DateTime date)
        {
            DateTime targetDate = date.Date;
            return await _dbContext.WorkoutSessionLogs
                .Where(l => (l.MemberId == memberOrUserId || l.UserId == memberOrUserId || (l.Member != null && l.Member.UserId == memberOrUserId)) && l.Date.Date == targetDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<WorkoutSessionLog>> GetLogsInRangeAsync(Guid memberOrUserId, DateTime startUtc, DateTime endUtc)
        {
            return await _dbContext.WorkoutSessionLogs
                .Where(l => (l.MemberId == memberOrUserId || l.UserId == memberOrUserId || (l.Member != null && l.Member.UserId == memberOrUserId))
                    && l.Date >= startUtc && l.Date < endUtc)
                .ToListAsync();
        }

        public async Task AddOrUpdateScheduleDayAsync(MemberWorkoutScheduleDay scheduleDay)
        {
            MemberWorkoutScheduleDay? existing = await _dbContext.MemberWorkoutScheduleDays
                .FirstOrDefaultAsync(s => s.MemberPlanAssignmentId == scheduleDay.MemberPlanAssignmentId && s.DayOfWeek == scheduleDay.DayOfWeek);

            if (existing != null)
            {
                existing.WorkoutPlanDayId = scheduleDay.WorkoutPlanDayId;
                existing.IsRestDay = scheduleDay.IsRestDay;
                existing.ModifiedOn = DateTime.UtcNow;
                _dbContext.MemberWorkoutScheduleDays.Update(existing);
            }
            else
            {
                await _dbContext.MemberWorkoutScheduleDays.AddAsync(scheduleDay);
            }
        }

        public async Task<IEnumerable<MemberPlanAssignment>> GetPlanAssignmentsAsync(Guid memberOrUserId)
        {
            return await _dbContext.MemberPlanAssignments
                .Include(mpa => mpa.CustomScheduleDays)
                .Include(mpa => mpa.WorkoutPlan)
                    .ThenInclude(p => p.Days)
                        .ThenInclude(d => d.Exercises)
                .Where(mpa => mpa.MemberId == memberOrUserId || mpa.UserId == memberOrUserId || (mpa.Member != null && mpa.Member.UserId == memberOrUserId))
                .OrderBy(mpa => mpa.AssignedAt)
                .ToListAsync();
        }
    }
}
