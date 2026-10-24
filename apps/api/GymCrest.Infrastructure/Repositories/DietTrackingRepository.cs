using GymCrest.Domain.Entities;
using GymCrest.Domain.Interface;
using GymCrest.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GymCrest.Infrastructure.Repositories
{
    public class DietTrackingRepository : IDietTrackingRepository
    {
        private readonly AppDbContext _context;

        public DietTrackingRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<DietLog?> GetDietLogWithEntriesAsync(Guid memberId, DateTime date)
        {
            DateTime start = DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);
            DateTime end = DateTime.SpecifyKind(start.AddDays(1), DateTimeKind.Utc);
            List<DietLog> logs = await _context.DietLogs
                .Include(d => d.MealEntries)
                .Where(d => d.MemberId == memberId && d.LogDate >= start && d.LogDate < end)
                .ToListAsync();

            return logs
                .OrderByDescending(d => d.MealEntries.Count)
                .ThenBy(d => d.CreatedOn)
                .FirstOrDefault();
        }

        public async Task<MealLogEntry?> GetMealEntryWithLogAsync(Guid mealEntryId)
        {
            return await _context.MealLogEntries
                .Include(e => e.DietLog)
                .FirstOrDefaultAsync(e => e.Id == mealEntryId);
        }

        public async Task<List<DietLog>> GetDietLogsByDateRangeAsync(Guid memberId, DateTime startDate, DateTime endDate)
        {
            return await _context.DietLogs
                .Where(d => d.MemberId == memberId && d.LogDate >= startDate && d.LogDate <= endDate)
                .OrderBy(d => d.LogDate)
                .ToListAsync();
        }

        public void AddDietLog(DietLog log)
        {
            _context.DietLogs.Add(log);
        }

        public void RemoveMealEntry(MealLogEntry entry)
        {
            _context.MealLogEntries.Remove(entry);
        }

        public async Task LoadMealEntriesAsync(DietLog log)
        {
            await _context.Entry(log).Collection(l => l.MealEntries).LoadAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
