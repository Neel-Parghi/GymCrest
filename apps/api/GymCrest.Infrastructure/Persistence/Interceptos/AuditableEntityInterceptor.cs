using GymCrest.Domain.Entities;
using GymCrest.Domain.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace GymCrest.Infrastructure.Persistence.Interceptos
{
    public class AuditableEntityInterceptor : SaveChangesInterceptor
    {
        private readonly ICurrentUserService _currentUserService;

        public AuditableEntityInterceptor(ICurrentUserService currentUserService)
        {
            _currentUserService = currentUserService;
        }

        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            UpdateEntities(eventData.Context);
            return base.SavingChanges(eventData, result);
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            UpdateEntities(eventData.Context);
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        public void UpdateEntities(DbContext? context)
        {
            if(context == null) return;
        
            try 
            {
                foreach (EntityEntry<BaseEntity> entry in context.ChangeTracker.Entries<BaseEntity>())
                {
                    DateTime now = DateTime.UtcNow;
                    Guid? userId = _currentUserService.UserId;

                    if (entry.State == EntityState.Added)
                    {
                        entry.Entity.CreatedOn = now;
                        entry.Entity.CreatedBy = userId ?? Guid.Empty;
                        
                        entry.Entity.ModifiedOn = now;
                        entry.Entity.ModifiedBy = userId;
                    }
                    else if (entry.State == EntityState.Modified || entry.HasChangedOwnedEntities())
                    {
                        entry.Entity.ModifiedOn = now;
                        entry.Entity.ModifiedBy = userId;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"INTERCEPTOR ERROR: {ex.Message}");
            }
        }
    }

    public static class Extensions
    {
        public static bool HasChangedOwnedEntities(this EntityEntry entry) =>
            entry.References.Any(r =>
                r.TargetEntry != null &&
                r.TargetEntry.Metadata.IsOwned() &&
                (r.TargetEntry.State == EntityState.Added || r.TargetEntry.State == EntityState.Modified));
    }
}
