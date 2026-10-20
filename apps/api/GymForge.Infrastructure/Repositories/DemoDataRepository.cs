using GymForge.Domain.Entities;
using GymForge.Domain.Interface;
using GymForge.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GymForge.Infrastructure.Repositories
{
    public class DemoDataRepository : IDemoDataRepository
    {
        private readonly AppDbContext _dbContext;

        public DemoDataRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _dbContext.Users
                .Include(u => u.Profile)
                .Include(u => u.Preference)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
        }

        public async Task ClearMemberActivityAsync(Guid userId)
        {
            // Logged exercises/sets, meal entries and schedule days go with their parents through cascade deletes.
            await _dbContext.WorkoutSessionLogs.Where(l => l.UserId == userId).ExecuteDeleteAsync();
            await _dbContext.DietLogs.Where(l => l.MemberId == userId).ExecuteDeleteAsync();
            await _dbContext.MemberMeasurements.Where(m => m.UserId == userId).ExecuteDeleteAsync();
            await _dbContext.MemberPlanAssignments.Where(a => a.UserId == userId).ExecuteDeleteAsync();
            await _dbContext.MemberDietAssignments.Where(a => a.UserId == userId).ExecuteDeleteAsync();
            await _dbContext.WorkoutPlans.Where(p => p.CreatedBy == userId && p.IsCustom).ExecuteDeleteAsync();
            await _dbContext.DietPlans.Where(p => p.CreatedBy == userId && p.IsCustom).ExecuteDeleteAsync();
        }

        public async Task<List<WorkoutPlanDay>> GetActivePlanDaysAsync(Guid userId)
        {
            Guid? planId = await _dbContext.MemberPlanAssignments
                .Where(a => a.UserId == userId && a.IsActive)
                .OrderByDescending(a => a.AssignedAt)
                .Select(a => (Guid?)a.WorkoutPlanId)
                .FirstOrDefaultAsync();

            if (planId == null) return [];

            return await _dbContext.WorkoutPlanDays
                .AsNoTracking()
                .Include(d => d.Exercises.OrderBy(e => e.SortOrder))
                .Where(d => d.WorkoutPlanId == planId)
                .OrderBy(d => d.DayIndex)
                .ToListAsync();
        }

        public async Task AddMeasurementsAsync(IEnumerable<MemberMeasurement> measurements)
        {
            await _dbContext.MemberMeasurements.AddRangeAsync(measurements);
        }
    }
}
