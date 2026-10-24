using GymCrest.Domain.Entities;

namespace GymCrest.Domain.Interface
{
    public interface IMemberWorkoutRepository
    {
        Task<MemberPlanAssignment?> GetActivePlanAssignmentAsync(Guid memberOrUserId);
        
        Task AddPlanAssignmentAsync(MemberPlanAssignment assignment);
        
        /// <summary>Logs with exercises and sets, newest first; optionally limited to [startUtc, endUtc).</summary>
        Task<IEnumerable<WorkoutSessionLog>> GetWorkoutLogsAsync(Guid memberOrUserId, DateTime? startUtc = null, DateTime? endUtc = null);
        
        Task AddWorkoutSessionLogAsync(WorkoutSessionLog log);
        
        Task RemoveWorkoutSessionLogsAsync(IEnumerable<WorkoutSessionLog> logs);
        
        Task<IEnumerable<WorkoutSessionLog>> GetLogsByDateAsync(Guid memberOrUserId, DateTime date);

        Task<IEnumerable<WorkoutSessionLog>> GetLogsInRangeAsync(Guid memberOrUserId, DateTime startUtc, DateTime endUtc);

        Task AddOrUpdateScheduleDayAsync(MemberWorkoutScheduleDay scheduleDay);

        Task<IEnumerable<MemberPlanAssignment>> GetPlanAssignmentsAsync(Guid memberOrUserId);

        Task<IEnumerable<MemberPlanAssignment>> GetAllActiveAssignmentsAsync();
    }
}
