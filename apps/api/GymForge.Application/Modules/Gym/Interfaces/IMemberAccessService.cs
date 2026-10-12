namespace GymForge.Application.Modules.Gym.Interfaces
{
    public interface IMemberAccessService
    {
        Task<bool> CanAccessMemberAsync(Guid memberId, Guid callerUserId, Guid? callerGymId, bool isGymOwner, bool isGymStaff);
    }
}
