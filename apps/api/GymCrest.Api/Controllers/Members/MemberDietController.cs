using GymCrest.Application.Modules.Diet.Interface;
using GymCrest.Contracts.DietPlan;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GymCrest.Api.Filters;

namespace GymCrest.Api.Controllers.Members
{
    [Route("api/members/{memberId}")]
    [Authorize(Roles = "GymOwner,Staff,Trainer,User")]
    [AuthorizeMemberAccess]
    [ApiController]
    public class MemberDietController : BaseApiController
    {
        private readonly IMemberDietService _memberDietService;

        public MemberDietController(IMemberDietService memberDietService)
        {
            _memberDietService = memberDietService;
        }

        [HttpGet("active-diet")]
        public async Task<ActionResult<MemberDietAssignmentDto>> GetActiveDiet(Guid memberId)
        {
            MemberDietAssignmentDto? assignment = await _memberDietService.GetActiveDietForMemberAsync(memberId);
            if (assignment == null)
            {
                return NotFound("No active diet plan assigned to this member.");
            }
            return Ok(assignment);
        }

        [HttpPost("assign-diet")]
        public async Task<ActionResult> AssignDiet(Guid memberId, [FromBody] AssignDietPlanRequest request)
        {
            bool success = await _memberDietService.AssignDietToMemberAsync(memberId, request.DietPlanId, IsStandaloneUser ? null : GymId);
            if (!success)
            {
                return BadRequest("Failed to assign diet plan.");
            }
            return Ok(new { message = "Diet plan assigned successfully." });
        }

        [HttpPost("custom-diet")]
        public async Task<ActionResult> SaveCustomDiet(Guid memberId, [FromBody] CreateDietPlanRequest request)
        {
            bool success = await _memberDietService.AssignCustomDietToMemberAsync(memberId, request, IsStandaloneUser ? null : GymId, UserId);
            if (!success)
            {
                return BadRequest("Failed to save and assign custom diet plan.");
            }
            return Ok(new { message = "Custom diet plan saved and assigned successfully." });
        }

        [HttpDelete("active-diet")]
        public async Task<ActionResult> UnassignDiet(Guid memberId)
        {
            bool success = await _memberDietService.UnassignActiveDietAsync(memberId, IsStandaloneUser ? null : GymId);
            if (!success)
            {
                return BadRequest("No active diet plan to remove.");
            }
            return Ok(new { message = "Diet plan unassigned successfully." });
        }
    }
}
