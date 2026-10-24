using GymCrest.Domain.Entities;
using GymCrest.Domain.Interface;
using GymCrest.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GymCrest.Infrastructure.Repositories
{
    public class MaintenanceRepository : IMaintenanceRepository
    {
        private readonly AppDbContext _dbContext;

        public MaintenanceRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddMaintenanceLogAsync(MaintenanceLog log) => await _dbContext.MaintenanceLogs.AddAsync(log);

        public async Task<MaintenanceLog?> GetMaintenanceLogByIdAsync(Guid id) => 
            await _dbContext.MaintenanceLogs.FindAsync(id);

        public void UpdateMaintenanceLog(MaintenanceLog log)
        {
            if (_dbContext.Entry(log).State == EntityState.Detached)
                _dbContext.MaintenanceLogs.Update(log);
        }

        public async Task<List<MaintenanceLog>> GetMaintenanceLogsByEquipmentIdAsync(Guid equipmentId)
        {
            return await _dbContext.MaintenanceLogs
                .Where(x => x.EquipmentId == equipmentId)
                .OrderByDescending(x => x.StartDate)
                .ToListAsync();
        }

        public async Task<List<MaintenanceLog>> GetAllMaintenanceLogsAsync(Guid gymId, Guid? branchId = null)
        {
            IQueryable<MaintenanceLog> query = _dbContext.MaintenanceLogs
                .Include(x => x.Equipment)
                .Where(x => x.Equipment.GymId == gymId);

            if (branchId.HasValue)
            {
                query = query.Where(x => x.Equipment.BranchId == branchId.Value || (x.Equipment.BranchId == null && _dbContext.Branches.Any(b => b.Id == branchId.Value && b.IsMainBranch)));
            }

            return await query
                .OrderByDescending(x => x.StartDate)
                .ToListAsync();
        }

        public async Task<(IEnumerable<MaintenanceLog> items, int totalCount)> GetPagedMaintenanceLogsAsync(Guid gymId, int pageNumber, int pageSize, string? searchTerm, Guid? branchId = null)
        {
            IQueryable<MaintenanceLog> query = _dbContext.MaintenanceLogs
                .Include(x => x.Equipment)
                .Where(x => x.Equipment.GymId == gymId);

            if (branchId.HasValue)
            {
                query = query.Where(x => x.Equipment.BranchId == branchId.Value || (x.Equipment.BranchId == null && _dbContext.Branches.Any(b => b.Id == branchId.Value && b.IsMainBranch)));
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                string lowerTerm = searchTerm.ToLower();
                query = query.Where(x => 
                    x.TechnicianName.ToLower().Contains(lowerTerm) || 
                    x.ServiceType.ToLower().Contains(lowerTerm) || 
                    x.Equipment.Name.ToLower().Contains(lowerTerm)
                );
            }

            int totalCount = await query.CountAsync();

            List<MaintenanceLog> items = await query
                .OrderByDescending(x => x.StartDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }
    }
}
