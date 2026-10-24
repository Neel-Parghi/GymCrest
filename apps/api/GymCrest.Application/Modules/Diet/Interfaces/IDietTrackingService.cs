using GymCrest.Contracts.DietTracking;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GymCrest.Application.Modules.Diet.Interfaces
{
    public interface IDietTrackingService
    {
        Task<DietLogDto> GetDietLogAsync(Guid memberId, DateTime logDate);
        Task<DietLogDto> AddMealEntryAsync(Guid memberId, AddMealEntryRequestDto request);
        Task<DietLogDto> UpdateMealEntryAsync(Guid memberId, Guid mealEntryId, UpdateMealEntryRequestDto request);
        Task<DietLogDto> RemoveMealEntryAsync(Guid memberId, Guid mealEntryId);
        Task<List<DietLogSummaryDto>> GetWeeklySummaryAsync(Guid memberId, DateTime endDate);
    }
}
