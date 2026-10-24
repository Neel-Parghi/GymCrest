using GymCrest.Domain.Entities;

namespace GymCrest.Domain.Interface
{
    public interface IDietPlanRepository
    {
        Task<IEnumerable<DietPlan>> GetPlansAsync(Guid? gymId, Guid userId, string? goal = null, bool restrictToOwnPlans = false);
        
        Task<DietPlan?> GetPlanByIdAsync(Guid id);
        
        Task<DietPlan> CreatePlanAsync(DietPlan plan);
        
        Task<bool> UpdatePlanAsync(DietPlan plan);
        
        Task<bool> DeletePlanAsync(Guid id);
    }
}
