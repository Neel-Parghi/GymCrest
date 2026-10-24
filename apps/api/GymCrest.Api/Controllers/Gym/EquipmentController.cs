using GymCrest.Contracts.Gym.Inventory;
using GymCrest.Application.Modules.Gym.Interfaces;
using GymCrest.Contracts.Common;
using GymCrest.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GymCrest.Api.Filters;
using GymCrest.Shared.Enums;

namespace GymCrest.Api.Controllers.Gym
{
    [Route("api/equipment")]
    [Authorize(Roles = "GymOwner,Staff")]
    [ApiController]
    public class EquipmentController : BaseApiController
    {
        private readonly IEquipmentService _equipmentService;

        public EquipmentController(IEquipmentService equipmentService)
        {
            _equipmentService = equipmentService;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResponse<EquipmentDto>>> GetEquipment([FromQuery] PaginationParams pagination)
        {
            if (GymId == null) return Unauthorized();
            return Ok(await _equipmentService.GetEquipmentAsync(GymId.Value, pagination, SecureBranchId));
        }

        [HttpPost]
        public async Task<ActionResult<EquipmentDto>> AddEquipment([FromBody] CreateEquipmentDto dto)
        {
            if (GymId == null) return Unauthorized();
            EquipmentDto result = await _equipmentService.AddEquipmentAsync(dto, GymId.Value);
            return Ok(result);
        }

        [AuthorizeResource(OwnedResource.Equipment)]
        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateEquipment(Guid id, [FromBody] CreateEquipmentDto dto)
        {
            bool success = await _equipmentService.UpdateEquipmentAsync(id, dto);
            if (!success) return NotFound();
            return Ok(new { message = "Equipment updated successfully" });
        }
    }
}
