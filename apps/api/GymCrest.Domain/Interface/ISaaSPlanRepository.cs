using GymCrest.Contracts.SaaSPlan;
using GymCrest.Domain.Entities;

namespace GymCrest.Domain.Interface
{
    public interface ISaaSPlanRepository
    {
        Task<List<Plan>> GetAllPlansAsync();
        
        Task<Plan?> GetPlanByIdAsync(Guid id);
        
        Task<Plan> AddPlanAsync(Plan createPlan);
        
        Plan UpdatePlanAsync(Plan updateSaaSPlan);
    
        Task<bool> DeletePlanAsync(Guid id);

        Task<bool> IsPlanInUseAsync(Guid planId);
    }
}
