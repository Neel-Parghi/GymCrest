using GymForge.Domain.Entities;
using GymForge.Shared.Enums;
using GymForge.Domain.Interface;
using GymForge.Infrastructure.Persistence;
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
            OwnedResource.Staff => _dbContext.Staff.AsNoTracking().Where(x => x.Id == id)
                .Select(x => new ResourceOwnership(
                    x.GymId,
                    _dbContext.Gyms.Where(g => g.Id == x.GymId).Select(g => (Guid?)g.OwnerUserId).FirstOrDefault(),
                    x.UserId, x.CreatedBy, false))
                .FirstOrDefaultAsync(),
            OwnedResource.InventoryItem => GymScopedAsync(_dbContext.InventoryItems, id),
            OwnedResource.SaleTransaction => GymScopedAsync(_dbContext.SaleTransactions, id),
            OwnedResource.Equipment => GymScopedAsync(_dbContext.Equipment, id),
            // Gym plans are owned by the user who created them (the gym owner), not by a gym.
            OwnedResource.GymPlan => _dbContext.GymPlans.AsNoTracking().Where(x => x.Id == id)
                .Select(x => new ResourceOwnership(null, x.GymOwnerId, x.GymOwnerId, x.CreatedBy, false, false))
                .FirstOrDefaultAsync(),
            OwnedResource.Branch => GymScopedAsync(_dbContext.Branches, id),
            OwnedResource.MaintenanceLog => _dbContext.MaintenanceLogs.AsNoTracking().Where(x => x.Id == id)
                .Select(x => new ResourceOwnership(
                    x.Equipment.GymId,
                    _dbContext.Gyms.Where(g => g.Id == x.Equipment.GymId).Select(g => (Guid?)g.OwnerUserId).FirstOrDefault(),
                    null, x.CreatedBy, false))
                .FirstOrDefaultAsync(),
            OwnedResource.WorkoutPlan => _dbContext.WorkoutPlans.AsNoTracking().Where(x => x.Id == id)
                .Select(x => new ResourceOwnership(
                    x.GymId,
                    _dbContext.Gyms.Where(g => g.Id == x.GymId).Select(g => (Guid?)g.OwnerUserId).FirstOrDefault(),
                    null, x.CreatedBy, x.IsCustom, x.GymId == null && !x.IsCustom))
                .FirstOrDefaultAsync(),
            OwnedResource.DietPlan => _dbContext.DietPlans.AsNoTracking().Where(x => x.Id == id)
                .Select(x => new ResourceOwnership(
                    x.GymId,
                    _dbContext.Gyms.Where(g => g.Id == x.GymId).Select(g => (Guid?)g.OwnerUserId).FirstOrDefault(),
                    null, x.CreatedBy, x.IsCustom, x.IsTemplate))
                .FirstOrDefaultAsync(),
            OwnedResource.DailyRoutine => _dbContext.DailyRoutines.AsNoTracking().Where(x => x.Id == id)
                .Select(x => new ResourceOwnership(null, null, x.UserId, x.CreatedBy, false))
                .FirstOrDefaultAsync(),
            _ => throw new ArgumentOutOfRangeException(nameof(resource), resource, null)
        };

        public Task<Guid?> GetGymOwnerUserIdAsync(Guid gymId) =>
            _dbContext.Gyms.AsNoTracking().Where(g => g.Id == gymId).Select(g => (Guid?)g.OwnerUserId).FirstOrDefaultAsync();

        /// <summary>Records with a required GymId column.</summary>
        private Task<ResourceOwnership?> GymScopedAsync<T>(IQueryable<T> set, Guid id) where T : BaseEntity =>
            set.AsNoTracking().Where(x => x.Id == id)
                .Select(x => new ResourceOwnership(
                    EF.Property<Guid>(x, "GymId"),
                    _dbContext.Gyms.Where(g => g.Id == EF.Property<Guid>(x, "GymId")).Select(g => (Guid?)g.OwnerUserId).FirstOrDefault(),
                    null, x.CreatedBy, false))
                .FirstOrDefaultAsync();
    }
}
