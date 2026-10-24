using GymCrest.Contracts.WorkoutPlan;

namespace GymCrest.Application.Modules.Workout.Interface
{
    public interface IMemberWorkoutService
    {
        Task<WorkoutPlanDto?> GetActivePlanForMemberAsync(Guid memberId);
        
        Task<bool> AssignPlanToMemberAsync(Guid memberId, Guid planId, Guid? gymId);
        
        /// <summary>
        /// Logged sessions, newest first. <paramref name="from"/> and <paramref name="to"/> are optional,
        /// inclusive IST calendar dates; leave both empty for the full history.
        /// </summary>
        Task<IEnumerable<WorkoutSessionLogDto>> GetWorkoutLogsForMemberAsync(Guid memberId, DateTime? from = null, DateTime? to = null);
        
        Task<WorkoutSessionLogDto> LogWorkoutSessionAsync(Guid memberId, LogWorkoutSessionRequest request, Guid? gymId);

        Task<bool> SaveRecurringOverrideAsync(Guid memberId, string dayOfWeek, Guid? workoutPlanDayId, bool isRestDay);

        Task<IEnumerable<MemberPlanAssignmentDto>> GetPlanAssignmentsForMemberAsync(Guid memberId);
    }
}
