using GymCrest.Application.Modules.Gym.Interfaces;
using GymCrest.Contracts.Common;
using GymCrest.Contracts.Staff;
using GymCrest.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GymCrest.Api.Filters;
using GymCrest.Api.Middlewares;
using GymCrest.Shared.Enums;

namespace GymCrest.Api.Controllers.Gym
{
    [Route("api/staff")]
    [Authorize(Roles = "GymOwner,Staff,Trainer,User")]
    [RequireModuleAccess("staff")]
    public class StaffController : BaseApiController
    {
        private readonly IStaffService _staffService;

        public StaffController(IStaffService staffService)
        {
            _staffService = staffService;
        }

        [Authorize(Roles = "GymOwner,Staff")]
        [HttpPost]
        [BlockDemoAccount]
        public async Task<ActionResult> AddStaff([FromBody] AddStaffRequest request)
        {
            if (GymId == null) return Unauthorized();
            try
            {
                if (!User.IsInRole("GymOwner"))
                {
                    request.BranchId = SecureBranchId;
                }

                StaffResponse response = await _staffService.AddStaffAsync(GymId.Value, request);
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ClientErrorMessage.For(ex) });
            }
        }

        [Authorize(Roles = "GymOwner,Staff,Trainer")]
        [HttpGet]
        public async Task<ActionResult> GetGymStaff([FromQuery] PaginationParams pagination)
        {
            if (GymId == null) return Unauthorized();
            return Ok(await _staffService.GetGymStaffAsync(GymId.Value, pagination, SecureBranchId));
        }

        [AuthorizeResource(OwnedResource.Staff)]
        [HttpGet("{id}")]
        public async Task<ActionResult> GetStaffById(Guid id)
        {
            StaffResponse? staff = await _staffService.GetStaffByIdAsync(id);
            if (staff == null) return NotFound();
            return Ok(staff);
        }

        [Authorize(Roles = "GymOwner,Staff")]
        [AuthorizeResource(OwnedResource.Staff)]
        [HttpPut("{id}")]
        [BlockDemoAccount]
        public async Task<ActionResult> UpdateStaff(Guid id, [FromBody] AddStaffRequest request)
        {
            try
            {
                if (!User.IsInRole("GymOwner"))
                {
                    request.BranchId = SecureBranchId;
                }

                await _staffService.UpdateStaffAsync(id, request);
                return Ok(new { message = "Staff updated successfully" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ClientErrorMessage.For(ex) });
            }
        }

        [Authorize(Roles = "GymOwner,Staff")]
        [AuthorizeResource(OwnedResource.Staff)]
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteStaff(Guid id)
        {
            await _staffService.DeleteStaffAsync(id);
            return Ok(new { message = "Staff deleted successfully" });
        }

        [Authorize(Roles = "GymOwner,Staff")]
        [AuthorizeResource(OwnedResource.Staff)]
        [AuthorizeMemberAccess]
        [HttpPost("{id}/assign-member/{memberId}")]
        public async Task<ActionResult> AssignTrainerToMember(Guid id, Guid memberId, [FromQuery] string? slot, [FromQuery] int? durationDays)
        {
            if (durationDays is < 1 or > 3650)
                return BadRequest(new { message = "Duration must be between 1 and 3650 days." });

            await _staffService.AssignTrainerToMemberAsync(id, memberId, slot, durationDays);
            return Ok(new { message = "Trainer assigned successfully" });
        }

        [Authorize(Roles = "GymOwner,Staff,Trainer")]
        [AuthorizeResource(OwnedResource.Staff)]
        [AuthorizeMemberAccess]
        [HttpPost("{id}/deallocate-member/{memberId}")]
        public async Task<ActionResult> DeallocateTrainerFromMember(Guid id, Guid memberId)
        {
            await _staffService.DeallocateMemberFromTrainerAsync(id, memberId);
            return Ok(new { message = "Member deallocated from trainer successfully" });
        }

        [AuthorizeResource(OwnedResource.Staff)]
        [HttpGet("{id}/members")]
        public async Task<ActionResult> GetAssignedMembers(Guid id)
        {
            return Ok(await _staffService.GetAssignedMembersAsync(id));
        }

        [AuthorizeMemberAccess]

        [HttpPost("~/api/members/{memberId}/measurements")]
        public async Task<ActionResult> RecordMeasurement(Guid memberId, [FromBody] AddMeasurementRequest request)
        {
            await _staffService.RecordMeasurementAsync(memberId, UserId, request, IsStandaloneUser ? null : GymId);
            return Ok(new { message = "Measurement recorded successfully" });
        }

        [AuthorizeMemberAccess]

        [HttpGet("~/api/members/{memberId}/measurements")]
        public async Task<ActionResult> GetMemberMeasurements(Guid memberId)
        {
            return Ok(await _staffService.GetMemberMeasurementsAsync(memberId));
        }

        [AuthorizeResource(OwnedResource.Staff)]
        [HttpPost("{id}/check-in")]
        public async Task<ActionResult> CheckInStaff(Guid id, [FromBody] StaffCheckInRequest request)
        {
            if (GymId == null) return Unauthorized();
            try
            {
                StaffResponse response = await _staffService.CheckInStaffAsync(id, GymId.Value, SecureBranchId, request.Notes);
                return Ok(response);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ClientErrorMessage.For(ex) });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ClientErrorMessage.For(ex) });
            }
        }

        [AuthorizeResource(OwnedResource.Staff)]
        [HttpPost("{id}/check-out")]
        public async Task<ActionResult> CheckOutStaff(Guid id)
        {
            if (GymId == null) return Unauthorized();
            try
            {
                StaffResponse response = await _staffService.CheckOutStaffAsync(id, GymId.Value, SecureBranchId);
                return Ok(response);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ClientErrorMessage.For(ex) });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ClientErrorMessage.For(ex) });
            }
        }

        [Authorize(Roles = "GymOwner,Staff,Trainer")]
        [HttpGet("attendance-logs")]
        public async Task<ActionResult> GetStaffAttendanceLogs([FromQuery] PaginationParams pagination, [FromQuery] Guid? staffId = null)
        {
            if (GymId == null) return Unauthorized();

            if (User.IsInRole("Trainer") || User.IsInRole("Staff"))
            {
                StaffResponse? staff = await _staffService.GetStaffByIdAsync(UserId);
                if (staff != null)
                {
                    staffId = staff.Id;
                }
                else
                {
                    return Ok(pagination.BypassPagination == true 
                        ? (object)new List<StaffAttendanceLogResponse>() 
                        : new PagedResponse<StaffAttendanceLogResponse>(new List<StaffAttendanceLogResponse>(), 0, pagination.PageNumber, pagination.PageSize));
                }
            }
            
            if (pagination.BypassPagination == true)
            {
                IEnumerable<StaffAttendanceLogResponse> allLogs = await _staffService.GetStaffAttendanceLogsAsync(GymId.Value, SecureBranchId, staffId);
                return Ok(allLogs);
            }
            
            PagedResponse<StaffAttendanceLogResponse> pagedLogs = await _staffService.GetStaffAttendanceLogsPagedAsync(GymId.Value, pagination, SecureBranchId, staffId);
            return Ok(pagedLogs);
        }
    }
}
