using GymCrest.Shared.Enums;

namespace GymCrest.Application.Modules.Gym.Interfaces
{
    /// <summary>The signed-in caller, as far as access checks need it.</summary>
    public sealed record ResourceCaller(Guid UserId, Guid? GymId, bool IsGymOwner, bool IsGymStaff);

    public interface IResourceAccessService
    {
        /// <summary>Whether the caller may read (or, with <paramref name="write"/>, change) the record.</summary>
        Task<bool> CanAccessAsync(OwnedResource resource, Guid id, ResourceCaller caller, bool write);
    }
}
