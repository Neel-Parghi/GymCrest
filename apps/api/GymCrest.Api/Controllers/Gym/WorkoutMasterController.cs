using GymCrest.Application.Modules.Workout.Interface;
using GymCrest.Contracts.Workout;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymCrest.Api.Controllers.Gym
{
    [Route("api/workout-master")]
    [Authorize]
    [ApiController]
    public class WorkoutMasterController : BaseApiController
    {
        private readonly IWorkoutService _workoutService;

        public WorkoutMasterController(IWorkoutService workoutService)
        {
            _workoutService = workoutService;
        }

        [HttpGet("categories")]
        public async Task<ActionResult> GetCategories()
        {
            List<string>? categories = await _workoutService.GetCategories();

            if (categories == null || !categories.Any())
            {
                return Ok(new List<string>
                {
                    "Back", "Biceps", "Chest", "Triceps", "Shoulders", "Legs", "Core", "Cardio"
                });
            }

            return Ok(categories);
        }

        [HttpGet("exercises")]
        public async Task<ActionResult> GetExercises([FromQuery] ExerciseFilterParams filters)
        {
            List<ExerciseDto> exercises = await _workoutService.GetExercisesAsync(
                filters.Category, filters.Equipment, filters.Search);

            return Ok(exercises);
        }

        [HttpGet("exercises/{category}")]
        public async Task<ActionResult> GetExercisesByCategory(string category)
        {
            if (string.IsNullOrWhiteSpace(category))
            {
                return BadRequest("Category cannot be empty.");
            }

            List<ExerciseDto> exercises = await _workoutService.GetExercisesByCategoryAsync(category);
            return Ok(exercises);
        }

        [HttpGet("exercise-by-slug/{slug}")]
        public async Task<ActionResult> GetExerciseBySlug(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
            {
                return BadRequest("Slug cannot be empty.");
            }

            ExerciseDto? exercise = await _workoutService.GetExerciseBySlugAsync(slug);

            if (exercise == null)
            {
                return NotFound($"Exercise with slug '{slug}' not found.");
            }

            return Ok(exercise);
        }
    }
}
