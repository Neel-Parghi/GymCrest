using GymCrest.Contracts.SaaSPlan;

namespace GymCrest.Application.Modules.Gym.Interfaces
{
    public interface ISaaSPlanService
    {
        Task<List<SaaSPlanDto>> GetAllPlansAsync();

        Task<SaaSPlanDto?> GetPlanByIdAsync(Guid id);

        Task<SaaSPlanDto> AddPlanAsync(CreateSaaSPlanDto createPlanDto);

        Task<SaaSPlanDto> UpdatePlanAsync(UpdateSaaSPlanDto updateSaaSPlanDto);

        Task<bool> DeletePlanAsync(Guid id);
    }
}
