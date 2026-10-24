using GymCrest.Contracts.WorkoutPlan;

namespace GymCrest.Application.Modules.Workout.Interface
{
    public interface IWorkoutPlanService
    {
        Task<IEnumerable<WorkoutPlanDto>> GetPlansAsync(Guid? gymId, Guid userId, string? type = null, bool restrictToOwnPlans = false);
        
        Task<WorkoutPlanDto?> GetPlanByIdAsync(Guid id);
        
        Task<WorkoutPlanDto> CreatePlanAsync(CreateWorkoutPlanRequest request, Guid? gymId, Guid createdById);
        
        Task<WorkoutPlanDto> UpdatePlanAsync(UpdateWorkoutPlanRequest request, Guid? gymId, Guid modifiedById);
        
        Task<bool> DeletePlanAsync(Guid id);
    }
}
