using GymCrest.Domain.Entities;
using GymCrest.Domain.Interface;
using GymCrest.Infrastructure.Extensions;
using GymCrest.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GymCrest.Infrastructure.Repositories
{
    public class EquipmentRepository : IEquipmentRepository
    {
        private readonly AppDbContext _dbContext;

        public EquipmentRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Equipment?> GetEquipmentByIdAsync(Guid id)
        {
            return await _dbContext.Equipment
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<List<Equipment>> GetEquipmentByGymIdAsync(Guid gymId, Guid? branchId = null)
        {
            IQueryable<Equipment> query = _dbContext.Equipment
                .AsNoTracking()
                .Where(x => x.GymId == gymId);

            query = query.WhereBranchContext(_dbContext.Branches, branchId);

            return await query
                .OrderByDescending(x => x.ModifiedOn ?? x.CreatedOn)
                .ToListAsync();
        }

        public async Task<(IEnumerable<Equipment> items, int totalCount)> GetPagedEquipmentAsync(Guid gymId, int pageNumber, int pageSize, string? searchTerm, Guid? branchId = null)
        {
            IQueryable<Equipment> query = _dbContext.Equipment
                .AsNoTracking()
                .Where(x => x.GymId == gymId);

            query = query.WhereBranchContext(_dbContext.Branches, branchId);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                string lowerTerm = searchTerm.ToLower();
                query = query.Where(x => x.Name.ToLower().Contains(lowerTerm) || x.Category.ToLower().Contains(lowerTerm));
            }

            int totalCount = await query.CountAsync();

            List<Equipment> items = await query
                .OrderByDescending(x => x.ModifiedOn ?? x.CreatedOn)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task AddEquipmentAsync(Equipment equipment)
        {
            await _dbContext.Equipment.AddAsync(equipment);
        }

        public Equipment UpdateEquipment(Equipment equipment)
        {
            if (_dbContext.Entry(equipment).State == EntityState.Detached)
                _dbContext.Equipment.Update(equipment);
            return equipment;
        }

        public async Task DeleteEquipmentAsync(Guid id)
        {
            Equipment? equipment = await _dbContext.Equipment.FindAsync(id);
            if (equipment != null)
            {
                _dbContext.Equipment.Remove(equipment);
            }
        }
    }
}
