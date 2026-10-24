using GymCrest.Domain.Entities;
using GymCrest.Domain.Interface;
using GymCrest.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GymCrest.Infrastructure.Repositories
{
    public class WorkoutPlanRepository : IWorkoutPlanRepository
    {
        private readonly AppDbContext _dbContext;

        public WorkoutPlanRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IEnumerable<WorkoutPlan>> GetPlansAsync(Guid? gymId, Guid userId, string? type = null, bool restrictToOwnPlans = false)
        {
            List<Guid> userGymIds = !gymId.HasValue ? await _dbContext.GymMembers.Where(m => m.UserId == userId).Select(m => m.GymId).ToListAsync() : [];

            IQueryable<WorkoutPlan> query = _dbContext.WorkoutPlans
                                            .AsNoTracking()
                                            .Include(p => p.Days.OrderBy(d => d.DayIndex))
                                                .ThenInclude(d => d.Exercises.OrderBy(e => e.SortOrder))
                                            .Where(p => 
                                                (
                                                    (gymId.HasValue && p.GymId == gymId.Value && (!p.IsCustom || p.CreatedBy == userId)) || 
                                                    (!gymId.HasValue && p.CreatedBy == userId && !p.GymId.HasValue) ||
                                                    (!gymId.HasValue && p.GymId.HasValue && userGymIds.Contains(p.GymId.Value) && !p.IsCustom)
                                                ) && !p.IsDeleted && (!restrictToOwnPlans || p.CreatedBy == userId)
                                            );

            if (!string.IsNullOrWhiteSpace(type))
            {
                query = query.Where(p => p.Type == type);
            }

            return await query.OrderByDescending(p => p.CreatedOn).ToListAsync();
        }

        public async Task<WorkoutPlan?> GetPlanByIdAsync(Guid id)
        {
            return await _dbContext.WorkoutPlans
                .Include(p => p.Days.OrderBy(d => d.DayIndex))
                    .ThenInclude(d => d.Exercises.OrderBy(e => e.SortOrder))
                .FirstOrDefaultAsync(p => p.Id == id);
        }   

        public async Task<List<WorkoutPlanDay>> GetPlanDaysByIdsAsync(IEnumerable<Guid> dayIds)
        {
            List<Guid> ids = dayIds.Distinct().ToList();
            if (ids.Count == 0) return [];

            return await _dbContext.WorkoutPlanDays
                .Include(d => d.Exercises.OrderBy(e => e.SortOrder))
                .Where(d => ids.Contains(d.Id))
                .ToListAsync();
        }

        public async Task<WorkoutPlan> CreatePlanAsync(WorkoutPlan plan)
        {
            await _dbContext.WorkoutPlans.AddAsync(plan);
            return plan;
        }

        public Task<bool> UpdatePlanAsync(WorkoutPlan plan)
        {
            _dbContext.WorkoutPlans.Update(plan);
            return Task.FromResult(true);
        }

        public async Task<bool> DeletePlanAsync(Guid id)
        {
            WorkoutPlan? plan = await _dbContext.WorkoutPlans.FindAsync(id);
            if (plan == null) return false;

            plan.IsDeleted = true;
            plan.DeletedAt = DateTime.UtcNow;

            _dbContext.WorkoutPlans.Update(plan);
            return true;
        }
    }
}
