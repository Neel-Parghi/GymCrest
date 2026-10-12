using GymForge.Application.Modules.Workout.Interface;
using GymForge.Contracts.WorkoutPlan;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GymForge.Api.Middlewares;
using GymForge.Api.Filters;
using GymForge.Shared.Enums;

namespace GymForge.Api.Controllers.Gym
{
    [Route("api/workout-plan")]
    [Authorize(Roles = "GymOwner,Staff,Trainer,User")]
    [ApiController]
    public class WorkoutPlanController : BaseApiController
    {
        private readonly IWorkoutPlanService _workoutPlanService;

        public WorkoutPlanController(IWorkoutPlanService workoutPlanService)
        {
            _workoutPlanService = workoutPlanService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<WorkoutPlanDto>>> GetPlans([FromQuery] string? type = null)
        {
            bool isTrainer = User.IsInRole("Trainer");
            IEnumerable<WorkoutPlanDto> plans = await _workoutPlanService.GetPlansAsync(GymId, UserId, type, isTrainer);
            return Ok(plans);
        }

        [AuthorizeResource(OwnedResource.WorkoutPlan)]
        [HttpGet("{id}")]
        public async Task<ActionResult<WorkoutPlanDto>> GetPlanById(Guid id)
        {
            WorkoutPlanDto? plan = await _workoutPlanService.GetPlanByIdAsync(id);
            if (plan == null) 
                return NotFound("Workout plan not found.");
            
            return Ok(plan);
        }

        [HttpPost]
        public async Task<ActionResult<WorkoutPlanDto>> CreatePlan([FromBody] CreateWorkoutPlanRequest request)
        {
            if (IsStandaloneUser)
            {
                request.IsCustom = true;
            }
            WorkoutPlanDto result = await _workoutPlanService.CreatePlanAsync(request, GymId, UserId);
            return Ok(result);
        }

        [AuthorizeResource(OwnedResource.WorkoutPlan)]
        [HttpPut("{id}")]
        public async Task<ActionResult<WorkoutPlanDto>> UpdatePlan(Guid id, [FromBody] UpdateWorkoutPlanRequest request)
        {
            if (id != request.Id) 
                return BadRequest("Plan ID mismatch.");

            try
            {
                WorkoutPlanDto result = await _workoutPlanService.UpdatePlanAsync(request, GymId, UserId);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ClientErrorMessage.For(ex));
            }
        }

        [AuthorizeResource(OwnedResource.WorkoutPlan)]
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeletePlan(Guid id)
        {
            bool success = await _workoutPlanService.DeletePlanAsync(id);
            if (!success) 
                return NotFound("Workout plan not found.");
            
            return Ok(new { message = "Workout plan deleted successfully." });
        }
    }
}
