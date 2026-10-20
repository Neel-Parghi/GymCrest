using GymForge.Domain.Entities;

namespace GymForge.Domain.Interface
{
    /// <summary>Data access for resetting the public demo member account.</summary>
    public interface IDemoDataRepository
    {
        Task<User?> GetUserByEmailAsync(string email);

        /// <summary>Deletes the user's workout and diet logs, measurements, plan assignments and own custom plans.</summary>
        Task ClearMemberActivityAsync(Guid userId);

        /// <summary>Days (with exercises) of the user's active workout plan, in plan order.</summary>
        Task<List<WorkoutPlanDay>> GetActivePlanDaysAsync(Guid userId);

        Task AddMeasurementsAsync(IEnumerable<MemberMeasurement> measurements);
    }
}
