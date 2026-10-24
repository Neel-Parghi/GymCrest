using GymCrest.Shared.Enums;

namespace GymCrest.Domain.Interface
{
    /// <summary>Who a record belongs to: its gym (and that gym's owner), its own user, and its creator.
    /// IsTemplate marks shared library plans that anyone may read.</summary>
    public sealed record ResourceOwnership(Guid? GymId, Guid? GymOwnerUserId, Guid? OwnerUserId, Guid CreatedBy, bool IsCustom, bool IsTemplate);

    public interface IResourceOwnershipRepository
    {
        /// <summary>Ownership of the record, or null when it does not exist.</summary>
        Task<ResourceOwnership?> GetOwnershipAsync(OwnedResource resource, Guid id);

        /// <summary>The user who owns the gym, or null when the gym does not exist.</summary>
        Task<Guid?> GetGymOwnerUserIdAsync(Guid gymId);
    }
}
