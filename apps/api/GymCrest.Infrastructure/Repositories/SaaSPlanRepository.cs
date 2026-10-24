using GymCrest.Domain.Entities;
using GymCrest.Domain.Interface;
using GymCrest.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GymCrest.Infrastructure.Repositories
{
    public class SaaSPlanRepository : ISaaSPlanRepository
    {
        private readonly AppDbContext _dbContext;
        
        public SaaSPlanRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Plan> AddPlanAsync(Plan plan)
        {
            await _dbContext.Plans.AddAsync(plan);
            return plan;
        }

        public async Task<bool> DeletePlanAsync(Guid id)
        {
            Plan? existingPlan = await _dbContext.Plans.FirstOrDefaultAsync(p => p.Id == id);
            if (existingPlan != null)
            {
                _dbContext.Plans.Remove(existingPlan);
                return true;
            }
            return false;
        }

        public async Task<List<Plan>> GetAllPlansAsync()
        {
             return await _dbContext.Plans.OrderByDescending(p => p.CreatedOn).ToListAsync();
        }

        public async Task<Plan?> GetPlanByIdAsync(Guid id)
        {
            return await _dbContext.Plans.FirstOrDefaultAsync(p => p.Id == id) ?? null;
        }

        public Plan UpdatePlanAsync(Plan updateSaaSPlan)
        {
            _dbContext.Plans.Update(updateSaaSPlan);
            return updateSaaSPlan;
        }

        public async Task<bool> IsPlanInUseAsync(Guid planId)
        {
            return await _dbContext.SubscriptionRecords.AnyAsync(s => s.PlanId == planId);
        }
    }
}
