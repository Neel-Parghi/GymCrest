using GymCrest.Application.Modules.Diet.Interfaces;
using GymCrest.Contracts.DietTracking;
using GymCrest.Domain.Entities;
using GymCrest.Domain.Interface;

namespace GymCrest.Application.Modules.Diet.Services
{
    public class DietTrackingService : IDietTrackingService
    {
        private readonly IDietTrackingRepository _repository;
        private readonly IMemberDietRepository _memberDietRepository;

        public DietTrackingService(IDietTrackingRepository repository, IMemberDietRepository memberDietRepository)
        {
            _repository = repository;
            _memberDietRepository = memberDietRepository;
        }

        public async Task<DietLogDto> GetDietLogAsync(Guid memberId, DateTime logDate)
        {
            DateTime date = DateTime.SpecifyKind(logDate.Date, DateTimeKind.Utc);
            MemberDietAssignment? activeAssignment = await _memberDietRepository.GetActiveDietAssignmentAsync(memberId);
            DietLog? log = await _repository.GetDietLogWithEntriesAsync(memberId, date);

            if (log == null)
            {
                log = await CreateEmptyLogAsync(memberId, date, activeAssignment);
            }
            else if (UpdateLogTotals(log))
            {
                await _repository.SaveChangesAsync();
            }

            return MapToDto(log, activeAssignment);
        }

        public async Task<DietLogDto> AddMealEntryAsync(Guid memberId, AddMealEntryRequestDto request)
        {
            DateTime date = DateTime.SpecifyKind(request.LogDate.Date, DateTimeKind.Utc);
            MemberDietAssignment? activeAssignment = await _memberDietRepository.GetActiveDietAssignmentAsync(memberId);
            DietLog? log = await _repository.GetDietLogWithEntriesAsync(memberId, date);

            if (log == null)
            {
                log = await CreateEmptyLogAsync(memberId, date, activeAssignment);
            }

            MealLogEntry entry = new MealLogEntry
            {
                DietLogId = log.Id,
                MealType = request.MealType,
                FoodName = request.FoodName,
                Calories = request.Calories,
                Protein = request.Protein,
                Carbs = request.Carbs,
                Fats = request.Fats,
                SourceDietPlanMealId = request.SourceDietPlanMealId
            };

            log.MealEntries.Add(entry);
            UpdateLogTotals(log);

            await _repository.SaveChangesAsync();
            
            // Reload entries from DB so the returned DTO has fully populated IDs
            await _repository.LoadMealEntriesAsync(log);
            
            return MapToDto(log, activeAssignment);
        }

        public async Task<DietLogDto> UpdateMealEntryAsync(Guid memberId, Guid mealEntryId, UpdateMealEntryRequestDto request)
        {
            MealLogEntry? entry = await _repository.GetMealEntryWithLogAsync(mealEntryId);

            if (entry == null || entry.DietLog?.MemberId != memberId)
            {
                throw new Exception("Meal entry not found or unauthorized.");
            }

            entry.FoodName = request.FoodName;
            entry.Calories = request.Calories;
            entry.Protein = request.Protein;
            entry.Carbs = request.Carbs;
            entry.Fats = request.Fats;

            DietLog log = entry.DietLog!;

            await _repository.LoadMealEntriesAsync(log);

            UpdateLogTotals(log);
            await _repository.SaveChangesAsync();

            MemberDietAssignment? activeAssignment = await _memberDietRepository.GetActiveDietAssignmentAsync(memberId);
            return MapToDto(log, activeAssignment);
        }

        public async Task<DietLogDto> RemoveMealEntryAsync(Guid memberId, Guid mealEntryId)
        {
            MealLogEntry? entry = await _repository.GetMealEntryWithLogAsync(mealEntryId);

            if (entry == null || entry.DietLog?.MemberId != memberId)
            {
                throw new Exception("Meal entry not found or unauthorized.");
            }

            DietLog log = entry.DietLog!;
            await _repository.LoadMealEntriesAsync(log);
            log.MealEntries.Remove(entry);
            _repository.RemoveMealEntry(entry);

            UpdateLogTotals(log);
            await _repository.SaveChangesAsync();

            MemberDietAssignment? activeAssignment = await _memberDietRepository.GetActiveDietAssignmentAsync(memberId);
            return MapToDto(log, activeAssignment);
        }

        public async Task<List<DietLogSummaryDto>> GetWeeklySummaryAsync(Guid memberId, DateTime endDate)
        {
            DateTime startDate = endDate.Date.AddDays(-6);

            List<DietLog> logs = await _repository.GetDietLogsByDateRangeAsync(memberId, startDate, endDate);

            List<DietLogSummaryDto> summary = new List<DietLogSummaryDto>();

            for (int i = 0; i <= 6; i++)
            {
                DateTime targetDate = startDate.AddDays(i);
                DietLog? log = logs.FirstOrDefault(l => l.LogDate.Date == targetDate.Date);

                if (log != null)
                {
                    summary.Add(new DietLogSummaryDto
                    {
                        LogDate = log.LogDate,
                        TargetCalories = log.TargetCalories,
                        TotalCalories = log.TotalCalories,
                        TargetProtein = log.TargetProtein,
                        TotalProtein = log.TotalProtein,
                        TargetCarbs = log.TargetCarbs,
                        TotalCarbs = log.TotalCarbs,
                        TargetFats = log.TargetFats,
                        TotalFats = log.TotalFats
                    });
                }
                else
                {
                    summary.Add(new DietLogSummaryDto
                    {
                        LogDate = targetDate,
                        TargetCalories = 0,
                        TotalCalories = 0
                    });
                }
            }

            return summary;
        }

        private async Task<DietLog> CreateEmptyLogAsync(Guid memberId, DateTime date, MemberDietAssignment? activeAssignment)
        {
            DietLog log = new DietLog
            {
                MemberId = memberId,
                LogDate = DateTime.SpecifyKind(date, DateTimeKind.Utc),
                TotalCalories = 0,
                TotalProtein = 0,
                TotalCarbs = 0,
                TotalFats = 0
            };

            // Sync targets from active diet plan if any

            if (activeAssignment?.DietPlan != null)
            {
                log.TargetCalories = activeAssignment.DietPlan.Calories;
                log.TargetProtein = activeAssignment.DietPlan.Protein;
                log.TargetCarbs = activeAssignment.DietPlan.Carbs;
                log.TargetFats = activeAssignment.DietPlan.Fats;
            }

            _repository.AddDietLog(log);
            await _repository.SaveChangesAsync();
            
            return log;
        }

        /// <summary>Recalculates the log totals from its entries. Returns true when any total changed.</summary>
        private bool UpdateLogTotals(DietLog log)
        {
            int calories = log.MealEntries.Sum(e => e.Calories);
            decimal protein = log.MealEntries.Sum(e => e.Protein);
            decimal carbs = log.MealEntries.Sum(e => e.Carbs);
            decimal fats = log.MealEntries.Sum(e => e.Fats);

            bool changed = log.TotalCalories != calories || log.TotalProtein != protein
                || log.TotalCarbs != carbs || log.TotalFats != fats;

            log.TotalCalories = calories;
            log.TotalProtein = protein;
            log.TotalCarbs = carbs;
            log.TotalFats = fats;
            return changed;
        }

        private DietLogDto MapToDto(DietLog log, MemberDietAssignment? activeAssignment)
        {
            int targetCalories = activeAssignment?.DietPlan != null ? activeAssignment.DietPlan.Calories : log.TargetCalories;
            decimal targetProtein = activeAssignment?.DietPlan != null ? activeAssignment.DietPlan.Protein : log.TargetProtein;
            decimal targetCarbs = activeAssignment?.DietPlan != null ? activeAssignment.DietPlan.Carbs : log.TargetCarbs;
            decimal targetFats = activeAssignment?.DietPlan != null ? activeAssignment.DietPlan.Fats : log.TargetFats;

            DietLogDto dto = new DietLogDto
            {
                Id = log.Id,
                MemberId = log.MemberId,
                LogDate = log.LogDate,
                TargetCalories = targetCalories,
                TargetProtein = targetProtein,
                TargetCarbs = targetCarbs,
                TargetFats = targetFats,
                TotalCalories = log.TotalCalories,
                TotalProtein = log.TotalProtein,
                TotalCarbs = log.TotalCarbs,
                TotalFats = log.TotalFats,
                MealEntries = log.MealEntries.Select(e => new MealLogEntryDto
                {
                    Id = e.Id,
                    MealType = e.MealType,
                    FoodName = e.FoodName,
                    Calories = e.Calories,
                    Protein = e.Protein,
                    Carbs = e.Carbs,
                    Fats = e.Fats,
                    SourceDietPlanMealId = e.SourceDietPlanMealId
                }).ToList()
            };

            if (activeAssignment?.DietPlan?.Meals != null)
            {
                dto.AssignedMeals = activeAssignment.DietPlan.Meals.Select(m => new AssignedMealDto
                {
                    Id = m.Id,
                    Name = m.Name,
                    Time = m.Time,
                    Calories = m.Calories,
                    Protein = m.Protein,
                    Carbs = m.Carbs,
                    Fats = m.Fats,
                    Items = m.Items
                }).ToList();
            }

            return dto;
        }
    }
}
