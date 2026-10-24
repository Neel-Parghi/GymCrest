using GymCrest.Application.Modules.Gym.Interfaces;
using GymCrest.Domain.Entities;
using GymCrest.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GymCrest.Infrastructure.Repositories
{
    public class GymAnnouncementRepository : IGymAnnouncementRepository
    {
        private readonly AppDbContext _context;

        public GymAnnouncementRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<GymAnnouncement> AddAsync(GymAnnouncement announcement)
        {
            await _context.GymAnnouncements.AddAsync(announcement);
            await _context.SaveChangesAsync();
            return announcement;
        }

        public async Task<GymAnnouncement?> GetByIdAsync(Guid id)
        {
            return await _context.GymAnnouncements.FindAsync(id);
        }

        public async Task<IEnumerable<GymAnnouncement>> GetAllByGymIdAsync(Guid gymId)
        {
            return await _context.GymAnnouncements
                .Where(a => a.GymId == gymId)
                .OrderByDescending(a => a.CreatedOn)
                .ToListAsync();
        }

        public async Task<IEnumerable<GymAnnouncement>> GetActiveByGymIdAsync(Guid gymId)
        {
            return await _context.GymAnnouncements
                .Where(a => a.GymId == gymId && a.IsActive)
                .OrderByDescending(a => a.CreatedOn)
                .ToListAsync();
        }

        public async Task UpdateAsync(GymAnnouncement announcement)
        {
            _context.GymAnnouncements.Update(announcement);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(GymAnnouncement announcement)
        {
            _context.GymAnnouncements.Remove(announcement);
            await _context.SaveChangesAsync();
        }
    }
}
