using GymForge.Application.Modules.Workout.Interface;
using GymForge.Contracts.WorkoutPlan;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GymForge.Api.Filters;

namespace GymForge.Api.Controllers.Members
{
    [Route("api/members/{memberId}")]
    [Authorize(Roles = "GymOwner,Staff,Trainer,User")]
    [AuthorizeMemberAccess]
    [ApiController]
    public class MemberWorkoutController : BaseApiController
    {
        private readonly IMemberWorkoutService _memberWorkoutService;

        public MemberWorkoutController(IMemberWorkoutService memberWorkoutService)
        {
            _memberWorkoutService = memberWorkoutService;
        }

        [HttpGet("active-plan")]
        public async Task<ActionResult<WorkoutPlanDto>> GetActivePlan(Guid memberId)
        {
            WorkoutPlanDto? plan = await _memberWorkoutService.GetActivePlanForMemberAsync(memberId);
            if (plan == null)
            {
                return NotFound("No active workout plan assigned to this member.");
            }
            return Ok(plan);
        }

        [HttpPost("assign-plan")]
        public async Task<ActionResult> AssignPlan(Guid memberId, [FromBody] AssignWorkoutPlanRequest request)
        {
            bool success = await _memberWorkoutService.AssignPlanToMemberAsync(memberId, request.WorkoutPlanId, IsStandaloneUser ? null : GymId);
            if (!success)
            {
                return BadRequest("Failed to assign workout plan.");
            }
            return Ok(new { message = "Workout plan assigned successfully." });
        }

        /// <param name="from">Optional first IST date (yyyy-MM-dd), inclusive.</param>
        /// <param name="to">Optional last IST date (yyyy-MM-dd), inclusive.</param>
        [HttpGet("workout-logs")]
        public async Task<ActionResult<IEnumerable<WorkoutSessionLogDto>>> GetWorkoutLogs(Guid memberId, [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
        {
            if (from.HasValue && to.HasValue && from.Value.Date > to.Value.Date)
            {
                return BadRequest("'from' must be on or before 'to'.");
            }

            IEnumerable<WorkoutSessionLogDto> logs = await _memberWorkoutService.GetWorkoutLogsForMemberAsync(memberId, from, to);
            return Ok(logs);
        }

        [HttpPost("workout-logs")]
        public async Task<ActionResult<WorkoutSessionLogDto>> LogWorkoutSession(Guid memberId, [FromBody] LogWorkoutSessionRequest request)
        {
            WorkoutSessionLogDto log = await _memberWorkoutService.LogWorkoutSessionAsync(memberId, request, IsStandaloneUser ? null : GymId);
            return Ok(log);
        }

        [HttpPost("recurring-override")]
        public async Task<ActionResult> SaveRecurringOverride(Guid memberId, [FromBody] SaveRecurringOverrideRequest request)
        {
            bool success = await _memberWorkoutService.SaveRecurringOverrideAsync(memberId, request.DayOfWeek, request.WorkoutPlanDayId, request.IsRestDay);
            if (!success)
            {
                return BadRequest("Failed to update recurring workout schedule.");
            }
            return Ok(new { message = "Recurring workout schedule updated successfully." });
        }

        [HttpGet("plan-assignments")]
        public async Task<ActionResult<IEnumerable<MemberPlanAssignmentDto>>> GetPlanAssignments(Guid memberId)
        {
            IEnumerable<MemberPlanAssignmentDto> assignments = await _memberWorkoutService.GetPlanAssignmentsForMemberAsync(memberId);
            return Ok(assignments);
        }
    }
}
