using GymForge.Api.Middlewares;
using GymForge.Application.Modules.Gym.Interfaces;
using GymForge.Contracts.Common;
using GymForge.Contracts.Members;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GymForge.Api.Filters;

namespace GymForge.Api.Controllers.Members
{
    [Route("api/members")]
    [Authorize(Roles = "GymOwner,Staff,Trainer")]
    [RequireModuleAccess("members")]
    public class MembersController : BaseApiController
    {
        private readonly IGymMemberService _memberService;

        public MembersController(IGymMemberService memberService)
        {
            _memberService = memberService;
        }

        [HttpPost]
        public async Task<ActionResult> OnboardMember([FromBody] OnboardMemberRequest request)
        {
            if (GymId == null) return Unauthorized();
            try
            {
                OnboardMemberRequest securedRequest = request;
                if (!User.IsInRole("GymOwner"))
                {
                    securedRequest = request with { BranchId = SecureBranchId };
                }

                GymMemberResponse response = await _memberService.OnboardMemberAsync(GymId.Value, securedRequest, UserId);
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ClientErrorMessage.For(ex) });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ClientErrorMessage.For(ex) });
            }
        }

        [HttpGet]
        public async Task<ActionResult> GetGymMembers([FromQuery] MemberFilterParams filter)
        {
            if (GymId == null) return Unauthorized();
            return Ok(await _memberService.GetGymMembersAsync(GymId.Value, filter, SecureBranchId));
        }

        [HttpGet("dashboard")]
        public async Task<ActionResult<MemberDashboardResponse>> GetMemberDashboardData()
        {
            if (GymId == null) return Unauthorized();
            return Ok(await _memberService.GetMemberDashboardDataAsync(GymId.Value, SecureBranchId));
        }

        [AuthorizeMemberAccess("id")]

        [HttpGet("{id}")]
        public async Task<ActionResult> GetMemberById(Guid id)
        {
            GymMemberResponse? member = await _memberService.GetMemberByIdAsync(id);
            if (member == null) return NotFound();
            return Ok(member);
        }
        
        [AuthorizeMemberAccess("id")]
        
        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateMember(Guid id, [FromBody] OnboardMemberRequest request)
        {
            try
            {
                OnboardMemberRequest securedRequest = request;
                if (!User.IsInRole("GymOwner"))
                {
                    securedRequest = request with { BranchId = SecureBranchId };
                }

                GymMemberResponse response = await _memberService.UpdateMemberAsync(id, securedRequest, UserId);
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ClientErrorMessage.For(ex) });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ClientErrorMessage.For(ex) });
            }
        }

        [AuthorizeMemberAccess("id")]

        [HttpPatch("{id}/status/toggle")]
        public async Task<ActionResult> ToggleMemberStatus(Guid id)
        {
            bool success = await _memberService.ToggleMemberStatusAsync(id);
            if (!success) return NotFound();
            return Ok(new { success = true });
        }

        [AuthorizeMemberAccess("id")]

        [HttpPatch("{id}/status/freeze")]
        public async Task<ActionResult> FreezeMember(Guid id, [FromBody] FreezeMemberRequest request)
        {
            try
            {
                bool success = await _memberService.FreezeMemberAsync(id, UserId, request.FreezeUntil);
                if (!success) return NotFound();
                return Ok(new { success = true });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ClientErrorMessage.For(ex) });
            }
        }

        [AuthorizeMemberAccess("id")]

        [HttpPatch("{id}/status/unfreeze")]
        public async Task<ActionResult> UnfreezeMember(Guid id)
        {
            bool success = await _memberService.UnfreezeMemberAsync(id, UserId);
            if (!success) return NotFound();
            return Ok(new { success = true });
        }

        [AuthorizeMemberAccess("id")]

        [HttpPost("{id}/subscriptions")]
        public async Task<ActionResult> RenewSubscription(Guid id, [FromBody] RenewSubscriptionRequest request)
        {
            try
            {
                GymMemberResponse response = await _memberService.RenewSubscriptionAsync(id, request, UserId);
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ClientErrorMessage.For(ex) });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ClientErrorMessage.For(ex) });
            }
        }

        [AuthorizeMemberAccess("id")]

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteMember(Guid id)
        {
            bool success = await _memberService.DeleteMemberAsync(id);
            if (!success) return NotFound();
            return Ok(new { success = true });
        }

        [AuthorizeMemberAccess("id")]

        [HttpGet("{id}/subscriptions")]
        public async Task<ActionResult> GetSubscriptionHistory(Guid id)
        {
            return Ok(await _memberService.GetSubscriptionHistoryAsync(id));
        }

        [HttpGet("export")]
        [SkipResponseWrapper]
        public async Task<IActionResult> ExportMembers()
        {
            if (GymId == null) return Unauthorized();
            byte[] bytes = await _memberService.ExportMembersAsync(GymId.Value, SecureBranchId);
            return File(bytes, "text/csv", $"Members_Export_{DateTime.UtcNow:yyyyMMdd}.csv");
        }
    }
}
