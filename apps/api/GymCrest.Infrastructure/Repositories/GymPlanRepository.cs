using GymCrest.Domain.Entities;
using GymCrest.Domain.Interface;
using GymCrest.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GymCrest.Infrastructure.Repositories
{
    public class GymPlanRepository : IGymPlanRepository
    {
        private readonly AppDbContext _dbContext;
        public GymPlanRepository(AppDbContext dbContext) 
        {
            _dbContext = dbContext;
        }

        public async Task<GymPlan> AddGymPlanAsync(GymPlan createGymPlan)
        {
            await _dbContext.GymPlans.AddAsync(createGymPlan);
            return createGymPlan;
        }

        public async Task<GymPlan?> GetPlanByIdAsync(Guid planId)
        {
            return await _dbContext.GymPlans.FirstOrDefaultAsync(x => x.Id == planId);
        }

        public async Task<IEnumerable<GymPlan>> GetPlansByOwnerIdAsync(Guid ownerId)
        {
            return await _dbContext.GymPlans.Where(x => x.GymOwnerId == ownerId).ToListAsync();
        }

        public GymPlan UpdateGymPlan(GymPlan updateGymPlan)
        {
            _dbContext.GymPlans.Update(updateGymPlan);
            return updateGymPlan;
        }

        public async Task<bool> DeleteGymPlanAsync(Guid planId)
        {
            bool hasMembers = await _dbContext.MemberSubscriptions.AnyAsync(s => s.GymPlanId == planId);
            if (hasMembers) return false;

            GymPlan? plan = await _dbContext.GymPlans.FindAsync(planId);
            if (plan == null) return false;

            _dbContext.GymPlans.Remove(plan);
            return true;
        }
    }
}
