using GymCrest.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GymCrest.Domain.Interface
{
    public interface IDietTrackingRepository
    {
        Task<DietLog?> GetDietLogWithEntriesAsync(Guid memberId, DateTime date);
        Task<MealLogEntry?> GetMealEntryWithLogAsync(Guid mealEntryId);
        Task<List<DietLog>> GetDietLogsByDateRangeAsync(Guid memberId, DateTime startDate, DateTime endDate);
        void AddDietLog(DietLog log);
        void RemoveMealEntry(MealLogEntry entry);
        Task LoadMealEntriesAsync(DietLog log);
        Task SaveChangesAsync();
    }
}
