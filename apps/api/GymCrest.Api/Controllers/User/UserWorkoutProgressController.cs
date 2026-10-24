using GymCrest.Application.Modules.Workout.Interface;
using GymCrest.Contracts.Workout;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymCrest.Api.Controllers.User
{
    [ApiController]
    [Route("api/user/workout-progress")]
    [Authorize(Roles = "User")]
    public class UserWorkoutProgressController : BaseApiController
    {
        private readonly IUserWorkoutProgressService _workoutProgressService;

        public UserWorkoutProgressController(IUserWorkoutProgressService workoutProgressService)
        {
            _workoutProgressService = workoutProgressService;
        }

        [HttpGet("exercises")]
        public async Task<IActionResult> GetLoggedExerciseNames()
        {
            IEnumerable<LoggedExerciseNameDto> names = await _workoutProgressService.GetLoggedExerciseNamesAsync(UserId);
            return Ok(names);
        }

        [HttpGet("muscle-groups")]
        public async Task<IActionResult> GetMuscleGroupProgress()
        {
            IEnumerable<MuscleGroupProgressDto> groups = await _workoutProgressService.GetMuscleGroupProgressAsync(UserId);
            return Ok(groups);
        }

        [HttpGet]
        public async Task<IActionResult> GetExerciseProgress([FromQuery] string exerciseName)
        {
            if (string.IsNullOrWhiteSpace(exerciseName))
                return BadRequest("exerciseName is required.");

            ExerciseProgressDto? progress = await _workoutProgressService.GetExerciseProgressAsync(UserId, exerciseName);
            if (progress == null)
                return NotFound();

            return Ok(progress);
        }
    }
}
