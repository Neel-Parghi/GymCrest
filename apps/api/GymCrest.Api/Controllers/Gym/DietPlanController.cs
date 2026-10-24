using GymCrest.Application.Modules.Diet.Interface;
using GymCrest.Contracts.DietPlan;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GymCrest.Api.Middlewares;
using GymCrest.Api.Filters;
using GymCrest.Shared.Enums;

namespace GymCrest.Api.Controllers.Gym
{
    [Route("api/diet-plan")]
    [Authorize(Roles = "GymOwner,Staff,Trainer,User")]
    [ApiController]
    public class DietPlanController : BaseApiController
    {
        private readonly IDietPlanService _dietPlanService;

        public DietPlanController(IDietPlanService dietPlanService)
        {
            _dietPlanService = dietPlanService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<DietPlanDto>>> GetPlans([FromQuery] string? goal = null)
        {
            bool isTrainer = User.IsInRole("Trainer");
            IEnumerable<DietPlanDto> plans = await _dietPlanService.GetPlansAsync(GymId, UserId, goal, isTrainer);
            return Ok(plans);
        }

        [AuthorizeResource(OwnedResource.DietPlan)]
        [HttpGet("{id}")]
        public async Task<ActionResult<DietPlanDto>> GetPlanById(Guid id)
        {
            DietPlanDto? plan = await _dietPlanService.GetPlanByIdAsync(id);
            if (plan == null) 
                return NotFound("Diet plan not found.");
            
            return Ok(plan);
        }

        [HttpPost]
        public async Task<ActionResult<DietPlanDto>> CreatePlan([FromBody] CreateDietPlanRequest request)
        {
            if (IsStandaloneUser)
            {
                request.IsCustom = true;
            }
            DietPlanDto result = await _dietPlanService.CreatePlanAsync(request, GymId, UserId);
            return Ok(result);
        }

        [AuthorizeResource(OwnedResource.DietPlan)]
        [HttpPut("{id}")]
        public async Task<ActionResult<DietPlanDto>> UpdatePlan(Guid id, [FromBody] UpdateDietPlanRequest request)
        {
            if (id != request.Id) 
                return BadRequest("Plan ID mismatch.");

            DietPlanDto? existingPlan = await _dietPlanService.GetPlanByIdAsync(id);
            if (existingPlan == null)
                return NotFound("Diet plan not found.");

            if (IsStandaloneUser && existingPlan.CreatedBy != UserId)
                return Forbid();

            try
            {
                DietPlanDto result = await _dietPlanService.UpdatePlanAsync(request, GymId, UserId);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ClientErrorMessage.For(ex));
            }
        }

        [AuthorizeResource(OwnedResource.DietPlan)]
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeletePlan(Guid id)
        {
            DietPlanDto? existingPlan = await _dietPlanService.GetPlanByIdAsync(id);
            if (existingPlan == null)
                return NotFound("Diet plan not found.");

            if (IsStandaloneUser && existingPlan.CreatedBy != UserId)
                return Forbid();

            bool success = await _dietPlanService.DeletePlanAsync(id);
            if (!success) 
                return NotFound("Diet plan not found.");
            
            return Ok(new { message = "Diet plan deleted successfully." });
        }
    }
}
