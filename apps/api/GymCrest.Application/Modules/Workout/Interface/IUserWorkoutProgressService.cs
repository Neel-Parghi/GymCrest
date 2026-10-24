using GymCrest.Contracts.Workout;

namespace GymCrest.Application.Modules.Workout.Interface
{
    public interface IUserWorkoutProgressService
    {
        Task<IEnumerable<LoggedExerciseNameDto>> GetLoggedExerciseNamesAsync(Guid userId);

        Task<ExerciseProgressDto?> GetExerciseProgressAsync(Guid userId, string exerciseName);

        Task<IEnumerable<MuscleGroupProgressDto>> GetMuscleGroupProgressAsync(Guid userId);
    }
}
