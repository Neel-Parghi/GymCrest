using GymCrest.Application.Modules.Gym.Interfaces;
using GymCrest.Contracts.Attendance;
using GymCrest.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GymCrest.Api.Middlewares;

namespace GymCrest.Api.Controllers.Attendance
{
    [Route("api/attendance")]
    [Authorize(Roles = "GymOwner,Staff")]
    public class AttendanceController : BaseApiController
    {
        private readonly IAttendanceService _attendanceService;

        public AttendanceController(IAttendanceService attendanceService)
        {
            _attendanceService = attendanceService;
        }

        [HttpPost("check-in")]
        public async Task<ActionResult> CheckIn([FromBody] CheckInRequest request)
        {
            if (GymId == null) 
                return Unauthorized();
            try
            {
                AttendanceLogResponse result = await _attendanceService.CheckInAsync(GymId.Value, SecureBranchId, request, UserId);
                return Ok(result);
            }
            catch (KeyNotFoundException ex) { 
                return NotFound(new { message = ClientErrorMessage.For(ex) }); 
            }
            catch (InvalidOperationException ex) { 
                return BadRequest(new { message = ClientErrorMessage.For(ex) }); 
            }
        }

        [HttpPost("check-out")]
        public async Task<ActionResult> CheckOut([FromBody] CheckOutRequest request)
        {
            if (GymId == null) 
                return Unauthorized();
            try
            {
                AttendanceLogResponse result = await _attendanceService.CheckOutAsync(GymId.Value, SecureBranchId, request, UserId);
                return Ok(result);
            }
            catch (InvalidOperationException ex) { 
                return BadRequest(new { message = ClientErrorMessage.For(ex) }); 
            }
        }

        [HttpGet("occupancy")]
        public async Task<ActionResult> GetOccupancy()
        {
            if (GymId == null) 
                return Unauthorized();

            IEnumerable<CheckedInMemberResponse> result = await _attendanceService.GetActiveOccupancyAsync(GymId.Value, SecureBranchId);
            return Ok(result);
        }

        [HttpGet("occupancy/stats")]
        public async Task<ActionResult> GetOccupancyStats()
        {
            if (GymId == null) 
                return Unauthorized();

            OccupancyStatsResponse result = await _attendanceService.GetOccupancyStatsAsync(GymId.Value, SecureBranchId);
            return Ok(result);
        }

        [HttpGet("logs")]
        public async Task<ActionResult> GetLogs([FromQuery] AttendanceLogQueryParams queryParams)
        {
            if (GymId == null) 
                return Unauthorized();
            
            PagedResponse<AttendanceLogResponse> result = await _attendanceService.GetLogsAsync(GymId.Value, SecureBranchId, queryParams);
            return Ok(result);
        }
    }
}
