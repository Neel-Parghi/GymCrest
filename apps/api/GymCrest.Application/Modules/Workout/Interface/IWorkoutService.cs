using GymCrest.Contracts.Workout;

namespace GymCrest.Application.Modules.Workout.Interface
{
    public interface IWorkoutService
    {
        Task<List<string>?> GetCategories();

        Task<List<ExerciseDto>> GetExercisesAsync(string? category, string? equipment, string? search);

        Task<List<ExerciseDto>> GetExercisesByCategoryAsync(string category);

        Task<ExerciseDto?> GetExerciseBySlugAsync(string slug);
    }
}
