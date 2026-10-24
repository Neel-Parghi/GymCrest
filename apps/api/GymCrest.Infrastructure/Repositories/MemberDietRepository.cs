using GymCrest.Domain.Entities;
using GymCrest.Domain.Interface;
using GymCrest.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GymCrest.Infrastructure.Repositories
{
    public class MemberDietRepository : IMemberDietRepository
    {
        private readonly AppDbContext _dbContext;

        public MemberDietRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<MemberDietAssignment?> GetActiveDietAssignmentAsync(Guid memberOrUserId)
        {
            return await _dbContext.MemberDietAssignments
                .Include(mda => mda.DietPlan)
                    .ThenInclude(dp => dp.Meals)
                .Where(mda => (mda.MemberId == memberOrUserId || mda.UserId == memberOrUserId || (mda.Member != null && mda.Member.UserId == memberOrUserId)) && mda.IsActive)
                .OrderByDescending(mda => mda.AssignedAt)
                .FirstOrDefaultAsync();
        }

        public async Task AddDietAssignmentAsync(MemberDietAssignment assignment)
        {
            await _dbContext.MemberDietAssignments.AddAsync(assignment);
        }

        public async Task<IEnumerable<MemberDietAssignment>> GetDietAssignmentsAsync(Guid memberOrUserId)
        {
            return await _dbContext.MemberDietAssignments
                .Include(mda => mda.DietPlan)
                    .ThenInclude(dp => dp.Meals)
                .Where(mda => mda.MemberId == memberOrUserId || mda.UserId == memberOrUserId || (mda.Member != null && mda.Member.UserId == memberOrUserId))
                .OrderByDescending(mda => mda.AssignedAt)
                .ToListAsync();
        }
    }
}
