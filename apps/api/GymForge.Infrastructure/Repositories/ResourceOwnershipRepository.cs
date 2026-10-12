using GymForge.Domain.Entities;
using GymForge.Shared.Enums;
using GymForge.Domain.Interface;
using GymForge.Infrastructure.Persistence;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace GymForge.Infrastructure.Repositories
{
    public class ResourceOwnershipRepository : IResourceOwnershipRepository
    {
        private readonly AppDbContext _dbContext;

        public ResourceOwnershipRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task<ResourceOwnership?> GetOwnershipAsync(OwnedResource resource, Guid id) => resource switch
        {
            OwnedResource.Staff => FirstAsync(_dbContext.Staff, id,
                x => new ResourceOwnership(x.GymId, x.Gym.OwnerUserId, x.UserId, x.CreatedBy, false, false)),
            OwnedResource.InventoryItem => GymScopedAsync(_dbContext.InventoryItems, id),
            OwnedResource.SaleTransaction => GymScopedAsync(_dbContext.SaleTransactions, id),
            OwnedResource.Equipment => GymScopedAsync(_dbContext.Equipment, id),
            OwnedResource.Branch => GymScopedAsync(_dbContext.Branches, id),
            OwnedResource.GymPlan => FirstAsync(_dbContext.GymPlans, id,
                x => new ResourceOwnership(null, x.GymOwnerId, x.GymOwnerId, x.CreatedBy, false, false)),
            OwnedResource.MaintenanceLog => FirstAsync(_dbContext.MaintenanceLogs, id,
                x => new ResourceOwnership(x.Equipment.GymId, x.Equipment.Gym.OwnerUserId, null, x.CreatedBy, false, false)),
            OwnedResource.WorkoutPlan => FirstAsync(_dbContext.WorkoutPlans, id,
                x => new ResourceOwnership(x.GymId,
                    _dbContext.Gyms.Where(g => g.Id == x.GymId).Select(g => (Guid?)g.OwnerUserId).FirstOrDefault(),
                    null, x.CreatedBy, x.IsCustom, x.GymId == null && !x.IsCustom)),
            OwnedResource.DietPlan => FirstAsync(_dbContext.DietPlans, id,
                x => new ResourceOwnership(x.GymId,
                    _dbContext.Gyms.Where(g => g.Id == x.GymId).Select(g => (Guid?)g.OwnerUserId).FirstOrDefault(),
                    null, x.CreatedBy, x.IsCustom, x.IsTemplate)),
            OwnedResource.DailyRoutine => FirstAsync(_dbContext.DailyRoutines, id,
                x => new ResourceOwnership(null, null, x.UserId, x.CreatedBy, false, false)),
            _ => throw new ArgumentOutOfRangeException(nameof(resource), resource, null)
        };

        public Task<Guid?> GetGymOwnerUserIdAsync(Guid gymId) =>
            _dbContext.Gyms.AsNoTracking().Where(g => g.Id == gymId).Select(g => (Guid?)g.OwnerUserId).FirstOrDefaultAsync();

        private static Task<ResourceOwnership?> FirstAsync<T>(IQueryable<T> set, Guid id,
            Expression<Func<T, ResourceOwnership?>> projection) where T : BaseEntity =>
            set.AsNoTracking().Where(x => x.Id == id).Select(projection).FirstOrDefaultAsync();

        private static Task<ResourceOwnership?> GymScopedAsync<T>(IQueryable<T> set, Guid id) where T : BaseEntity =>
            FirstAsync(set, id, x => new ResourceOwnership(
                EF.Property<Guid>(x, "GymId"), EF.Property<Gym>(x, "Gym").OwnerUserId, null, x.CreatedBy, false, false));
    }
}
