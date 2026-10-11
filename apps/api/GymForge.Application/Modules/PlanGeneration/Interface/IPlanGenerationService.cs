using GymForge.Contracts.PlanGeneration;

namespace GymForge.Application.Modules.PlanGeneration.Interface
{
    public interface IPlanGenerationService
    {
        Task<StarterPlanResultDto> GenerateStarterPlansAsync(Guid userId, StarterPlanProfileDto profile);

        StarterPlanResultDto CalculateTargets(StarterPlanProfileDto profile);
    }
}
