using GymCrest.Contracts.GymPlans;
using GymCrest.Domain.Entities;

namespace GymCrest.Domain.Interface
{
    public interface IGymPlanRepository
    {
        Task<IEnumerable<GymPlan>> GetPlansByOwnerIdAsync(Guid ownerId);

        Task<GymPlan?> GetPlanByIdAsync(Guid planId);

        Task<GymPlan> AddGymPlanAsync(GymPlan createGymPlan);

        GymPlan UpdateGymPlan(GymPlan updateGymPlan);

        Task<bool> DeleteGymPlanAsync(Guid planId);
    }
}
