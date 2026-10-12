namespace GymForge.Application.Modules.Gym.Interfaces
{
    public interface IMemberAccessService
    {
        /// <summary>
        /// Whether the caller may read or change a member's data: their own account or linked membership,
        /// a member of a gym they own (owners), or a member of their gym (staff and trainers).
        /// </summary>
        Task<bool> CanAccessMemberAsync(Guid memberId, Guid callerUserId, Guid? callerGymId, bool isGymOwner, bool isGymStaff);
    }
}
