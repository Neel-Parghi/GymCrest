using GymCrest.Application.Modules.Diet.Interfaces;
using GymCrest.Application.Modules.Gym.Interfaces;
using GymCrest.Contracts.DietTracking;
using GymCrest.Contracts.Staff;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using GymCrest.Api.Filters;

namespace GymCrest.Api.Controllers.Trainer
{
    [ApiController]
    [Route("api/trainer/diet-tracking")]
    [Authorize(Roles = "Trainer,GymOwner")]
    public class TrainerDietTrackingController : BaseApiController
    {
        private readonly IDietTrackingService _dietTrackingService;
        private readonly IStaffService _staffService;

        public TrainerDietTrackingController(IDietTrackingService dietTrackingService, IStaffService staffService)
        {
            _dietTrackingService = dietTrackingService;
            _staffService = staffService;
        }

        private async Task<bool> IsAuthorizedToViewMemberAsync(Guid memberId)
        {
            string? role = User.FindFirstValue(ClaimTypes.Role);
            Guid currentUserId = UserId;
            if (currentUserId == Guid.Empty) return false;

            if (role == "Trainer")
            {
                IEnumerable<TrainerMemberResponse> assignedMembers = await _staffService.GetAssignedMembersAsync(currentUserId);
                return assignedMembers.Any(m => m.MemberId == memberId);
            }
            
            if (role == "GymOwner" || role == "Staff")
            {
                return true;
            }

            return false;
        }

        [AuthorizeMemberAccess]

        [HttpGet("{memberId}/{date}")]
        public async Task<IActionResult> GetMemberDietLog(Guid memberId, DateTime date)
        {
            if (!await IsAuthorizedToViewMemberAsync(memberId)) return Forbid();
            DietLogDto log = await _dietTrackingService.GetDietLogAsync(memberId, date);
            return Ok(log);
        }

        [AuthorizeMemberAccess]

        [HttpGet("{memberId}/summary/{endDate}")]
        public async Task<IActionResult> GetMemberWeeklySummary(Guid memberId, DateTime endDate)
        {
            if (!await IsAuthorizedToViewMemberAsync(memberId)) return Forbid();
            List<DietLogSummaryDto> summary = await _dietTrackingService.GetWeeklySummaryAsync(memberId, endDate);
            return Ok(summary);
        }
    }
}
