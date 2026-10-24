using GymCrest.Domain.Entities;
using GymCrest.Domain.Interface;
using GymCrest.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GymCrest.Infrastructure.Repositories
{
    public class AnnouncementTemplateRepository : IAnnouncementTemplateRepository
    {
        private readonly AppDbContext _dbContext;

        public AnnouncementTemplateRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<AnnouncementTemplate?> GetByIdAsync(Guid id)
        {
            return await _dbContext.AnnouncementTemplates.FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<List<AnnouncementTemplate>> GetByGymIdAsync(Guid gymId, Guid? branchId = null)
        {
            IQueryable<AnnouncementTemplate> query = _dbContext.AnnouncementTemplates.Where(t => t.GymId == gymId);
            
            if (branchId.HasValue)
            {
                query = query.Where(t => t.BranchId == branchId.Value || t.BranchId == null);
            }

            return await query.ToListAsync();
        }

        public async Task AddAsync(AnnouncementTemplate template)
        {
            await _dbContext.AnnouncementTemplates.AddAsync(template);
            await _dbContext.SaveChangesAsync();
        }

        public void Update(AnnouncementTemplate template)
        {
            _dbContext.AnnouncementTemplates.Update(template);
            _dbContext.SaveChanges();
        }

        public async Task DeleteAsync(Guid id)
        {
            AnnouncementTemplate? template = await GetByIdAsync(id);
            if (template != null)
            {
                _dbContext.AnnouncementTemplates.Remove(template);
                await _dbContext.SaveChangesAsync();
            }
        }
    }
}
