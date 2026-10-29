using GymCrest.Application.Modules.Gym.Interfaces;
using GymCrest.Contracts.Common;
using GymCrest.Contracts.Gym.Owners;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymCrest.Api.Controllers.Gym
{
    [Route("api/gym-owners")]
    [Authorize(Roles = "SuperAdmin")]
    [ApiController]
    public class GymOwnerController : ControllerBase
    {
        private readonly IGymManagementService _gymManagementService;

        public GymOwnerController(IGymManagementService gymManagementService)
        {
            _gymManagementService = gymManagementService;
        }

        [HttpGet]
        public async Task<ActionResult> GetGymOwnersList([FromQuery] GymOwnerFilterParams filters)
        {
            return Ok(await _gymManagementService.GetGymOwnersList(filters));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateGymOwner(Guid id, [FromBody] UpdateGymOwnerDto updateGymOwnerDto)
        {
            if (id != updateGymOwnerDto.Id)
            {
                return BadRequest("ID mismatch between URL and payload.");
            }

            GymOwnersDto user = await _gymManagementService.UpdateGymOwner(updateGymOwnerDto);
            return Ok(user);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteGymOwner(Guid id)
        {
            bool deleted = await _gymManagementService.DeleteGymOwnerAsync(id);
            if (!deleted)
            {
                return Ok(new { Success = false, Message = "OWNER_HAS_GYMS" });
            }
            return Ok(new { Success = true, Message = "Gym Owner deleted successfully" });
        }
    }
}
