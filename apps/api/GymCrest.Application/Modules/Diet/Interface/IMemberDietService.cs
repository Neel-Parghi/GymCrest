using GymCrest.Contracts.DietPlan;

namespace GymCrest.Application.Modules.Diet.Interface
{
    public interface IMemberDietService
    {
        Task<MemberDietAssignmentDto?> GetActiveDietForMemberAsync(Guid memberId);
        
        Task<bool> AssignDietToMemberAsync(Guid memberId, Guid dietPlanId, Guid? gymId);
        
        Task<bool> AssignCustomDietToMemberAsync(Guid memberId, CreateDietPlanRequest request, Guid? gymId, Guid createdById);

        Task<bool> UnassignActiveDietAsync(Guid memberId, Guid? gymId);
    }
}
