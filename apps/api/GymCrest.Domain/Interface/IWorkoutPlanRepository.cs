using GymCrest.Domain.Entities;

namespace GymCrest.Domain.Interface
{
    public interface IWorkoutPlanRepository
    {
        Task<IEnumerable<WorkoutPlan>> GetPlansAsync(Guid? gymId, Guid userId, string? type = null, bool restrictToOwnPlans = false);
        
        Task<WorkoutPlan?> GetPlanByIdAsync(Guid id);

        /// <summary>Plan days with their exercises, from any plan.</summary>
        Task<List<WorkoutPlanDay>> GetPlanDaysByIdsAsync(IEnumerable<Guid> dayIds);
        
        Task<WorkoutPlan> CreatePlanAsync(WorkoutPlan plan);
        
        Task<bool> UpdatePlanAsync(WorkoutPlan plan);
        
        Task<bool> DeletePlanAsync(Guid id);
    }
}
