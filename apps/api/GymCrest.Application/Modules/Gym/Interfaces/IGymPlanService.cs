using GymCrest.Contracts.GymPlans;

namespace GymCrest.Application.Modules.Gym.Interfaces
{
    public interface IGymPlanService
    {
        Task<IEnumerable<GymPlanDto>> GetPlansByOwnerIdAsync(Guid ownerId);

        Task<GymPlanDto?> GetPlanByIdAsync(Guid planId);

        Task<GymPlanDto> AddGymPlanAsync(CreateGymPlanRequest createGymPlan);

        Task<GymPlanDto> UpdateGymPlanAsync(UpdateGymPlanRequest updateGymPlan);

        Task<bool> DeleteGymPlanAsync(Guid planId);

        Task<bool> PromotePlanAsync(Guid planId, Guid ownerId);

        Task<IEnumerable<GymPlanDto>> GetAvailablePlansForMemberAsync(Guid userId);
    }
}
