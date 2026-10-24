using Microsoft.AspNetCore.Authorization;
using GymCrest.Application.Modules.Gym.Interfaces;
using GymCrest.Contracts.SaaSPlan;
using Microsoft.AspNetCore.Mvc;

namespace GymCrest.Api.Controllers.Gym
{
    [Route("api/saas-plans")]
    [ApiController]
    public class SaaSPlanController : ControllerBase
    {
        private readonly ISaaSPlanService _saaSPlanService;
        public SaaSPlanController(ISaaSPlanService saaSPlanService)
        {
            _saaSPlanService = saaSPlanService;
        }

        [HttpGet]
        public async Task<IActionResult> GetPricingListAsync() 
        {
            return Ok(await _saaSPlanService.GetAllPlansAsync());
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPlanByIdAsync(Guid id)
        {
            SaaSPlanDto? plan = await _saaSPlanService.GetPlanByIdAsync(id);
            if (plan == null) return NotFound();
            return Ok(plan);
        }

        [Authorize(Roles = "SuperAdmin")]
        [HttpPost]
        public async Task<IActionResult> AddPlanAsync([FromBody] CreateSaaSPlanDto createPlanDto)
        {
            return Ok(await _saaSPlanService.AddPlanAsync(createPlanDto));
        }

        [Authorize(Roles = "SuperAdmin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePlan(Guid id, [FromBody] UpdateSaaSPlanDto updateSaaSPlanDto)
        {
            if (id != updateSaaSPlanDto.Id) return BadRequest("ID mismatch.");

            SaaSPlanDto updatedPlan = await _saaSPlanService.UpdatePlanAsync(updateSaaSPlanDto);
            return Ok(updatedPlan);
        }

        [Authorize(Roles = "SuperAdmin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePlanAsync(Guid id)
        {
            bool deleted = await _saaSPlanService.DeletePlanAsync(id);
            if (!deleted)
            {
                return Ok(new { Success = false, Message = "PLAN_IN_USE" });
            }
            return Ok(new { Success = true, Message = "Plan deleted successfully" });
        }
    }
}
