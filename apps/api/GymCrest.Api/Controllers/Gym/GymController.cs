using GymCrest.Application.Modules.Gym.Interfaces;
using GymCrest.Contracts.Common;
using GymCrest.Contracts.Gym.Management;
using GymCrest.Contracts.Gym.Onboarding;
using GymCrest.Contracts.Gym.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymCrest.Api.Controllers.Gym
{
    [Route("api/gyms")]
    [Authorize(Roles = "SuperAdmin")]
    [ApiController]
    public class GymController : ControllerBase
    {
        private readonly IGymManagementService _gymManagementService;
        public GymController(IGymManagementService gymManagementService)
        {
            _gymManagementService = gymManagementService;
        }

        [HttpPost("onboard")]
        public async Task<ActionResult> GymOnBoarding([FromBody] GymOnboardingDto dto)
        {
            if (dto == null) return BadRequest("Invalid request data.");

            Guid ownerId = dto.AssignedOwnerId ?? Guid.Empty;
            
            if (ownerId == Guid.Empty) return BadRequest("Owner assignment is required.");

            Guid newGymId = await _gymManagementService.OnboardGymAsync(ownerId, dto, activateImmediately: true);

            return Ok(new { message = "Gym onboarded successfully", gymId = newGymId });
        }

        [HttpGet]
        public async Task<ActionResult> GetGymList([FromQuery] PaginationParams pagination)
        {
            return Ok(await _gymManagementService.GetGymListAsync(pagination));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateGym(Guid id, [FromBody] UpdateGymDto updateGymDto)
        {
            if (id != updateGymDto.Id)
            {
                return BadRequest("ID mismatch.");
            }

            await _gymManagementService.UpdateGymAsync(updateGymDto);
            return Ok(new { message = "Gym updated successfully" });
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteGym(Guid id)
        {
            bool deleted = await _gymManagementService.DeleteGymAsync(id);
            if (!deleted)
            {
                return Ok(new { Success = false, Message = "GYM_HAS_BRANCHES" });
            }
            return Ok(new { Success = true, Message = "Gym deleted successfully" });
        }

        [HttpGet("{id}/branches")]
        public async Task<ActionResult> GetBranches(Guid id)
        {
            return Ok(await _gymManagementService.GetBranchesByGymIdAsync(id));
        }

        [HttpPost("{id}/branches")]
        public async Task<IActionResult> AddBranch(Guid id, [FromBody] BranchDto branchDto)
        {
            await _gymManagementService.AddBranchAsync(id, branchDto);
            return Ok(new { message = "Branch added successfully" });
        }
    }
}
