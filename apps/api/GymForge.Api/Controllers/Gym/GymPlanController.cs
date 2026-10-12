using GymForge.Application.Modules.Gym.Interfaces;
using GymForge.Contracts.GymPlans;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using GymForge.Api.Filters;
using GymForge.Shared.Enums;
namespace GymForge.Api.Controllers.Gym
{
    [Route("api/gym-plans")]
    [Authorize(Roles = "GymOwner,Staff")]
    [RequireModuleAccess("plans")]
    [AllowExpiredSubscription]
    public class GymPlanController : BaseApiController
    {
        private readonly IGymPlanService _gymPlanService;

        public GymPlanController(IGymPlanService gymPlanService) 
        {
            _gymPlanService = gymPlanService;
        }

        [HttpGet]
        public async Task<ActionResult> GetGymPlans()
        {
            return Ok(await _gymPlanService.GetPlansByOwnerIdAsync(UserId));
        }

        [AuthorizeResource(OwnedResource.GymPlan)]
        [HttpGet("{id}")]
        public async Task<ActionResult> GetGymPlanById(Guid id)
        {
            return Ok(await _gymPlanService.GetPlanByIdAsync(id));
        }

        [HttpPost]
        public async Task<ActionResult> AddGymPlanAsync([FromBody] CreateGymPlanRequest gymPlan)
        {
            gymPlan.GymOwnerId = UserId;
            return Ok(await _gymPlanService.AddGymPlanAsync(gymPlan));
        }

        [AuthorizeResource(OwnedResource.GymPlan)]
        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateGymPlanAsync(Guid id, [FromBody] UpdateGymPlanRequest gymPlan)
        {
            if (id != gymPlan.Id) return BadRequest("ID mismatch");
            
            gymPlan.GymOwnerId = UserId;
            return Ok(await _gymPlanService.UpdateGymPlanAsync(gymPlan));
        }

        [AuthorizeResource(OwnedResource.GymPlan)]
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteGymPlanAsync(Guid id)
        {
            bool deleted = await _gymPlanService.DeleteGymPlanAsync(id);
            if (!deleted)
            {
                return Ok(new { Success = false, Message = "PLAN_HAS_MEMBERS" });
            }
            return Ok(new { Success = true, Message = "Plan deleted successfully" });
        }

        [AuthorizeResource(OwnedResource.GymPlan)]
        [HttpPost("{id}/promote")]
        public async Task<ActionResult> PromoteGymPlanAsync(Guid id)
        {
            bool success = await _gymPlanService.PromotePlanAsync(id, UserId);
            
            if (!success) 
                return BadRequest("Unable to promote plan.");
            
            return Ok(new { Success = true, Message = "Plan promoted successfully" });
        }
    }
}
