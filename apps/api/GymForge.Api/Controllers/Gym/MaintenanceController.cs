using GymForge.Contracts.Gym.Inventory;
using GymForge.Application.Modules.Gym.Interfaces;
using GymForge.Contracts.Common;
using GymForge.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GymForge.Api.Filters;
using GymForge.Shared.Enums;

namespace GymForge.Api.Controllers.Gym
{
    [Route("api/maintenance")]
    [Authorize(Roles = "GymOwner,Staff")]
    [ApiController]
    public class MaintenanceController : BaseApiController
    {
        private readonly IMaintenanceService _maintenanceService;

        public MaintenanceController(IMaintenanceService maintenanceService)
        {
            _maintenanceService = maintenanceService;
        }

        [HttpGet("history")]
        public async Task<ActionResult<PagedResponse<MaintenanceLogDto>>> GetAllMaintenanceHistory([FromQuery] PaginationParams pagination)
        {
            if (GymId == null) return Unauthorized();
            return Ok(await _maintenanceService.GetAllMaintenanceLogsAsync(GymId.Value, pagination, SecureBranchId));
        }

        [HttpPost]
        public async Task<ActionResult> LogMaintenance([FromBody] LogMaintenanceDto dto)
        {
            bool success = await _maintenanceService.LogMaintenanceAsync(dto);
            if (!success) return BadRequest(new { message = "Failed to log maintenance" });
            return Ok(new { message = "Maintenance logged successfully" });
        }

        [AuthorizeResource(OwnedResource.MaintenanceLog)]
        [AuthorizeResource(OwnedResource.Equipment, "dto.EquipmentId")]
        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateMaintenance(Guid id, [FromBody] LogMaintenanceDto dto)
        {
            dto.Id = id;
            bool success = await _maintenanceService.LogMaintenanceAsync(dto);
            if (!success) return BadRequest(new { message = "Failed to update maintenance" });
            return Ok(new { message = "Maintenance updated successfully" });
        }

        [AuthorizeResource(OwnedResource.Equipment)]
        [HttpGet("equipment/{id}/maintenance")]
        public async Task<ActionResult<List<MaintenanceLogDto>>> GetEquipmentMaintenanceHistory(Guid id)
        {
            return Ok(await _maintenanceService.GetMaintenanceHistoryAsync(id));
        }
    }
}
