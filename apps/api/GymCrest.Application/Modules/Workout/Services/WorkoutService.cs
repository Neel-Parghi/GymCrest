using AutoMapper;
using GymCrest.Application.Modules.Workout.Interface;
using GymCrest.Contracts.Workout;
using GymCrest.Domain.Entities;
using GymCrest.Domain.Interface;

namespace GymCrest.Application.Modules.Workout.Services
{
    public class WorkoutService : IWorkoutService
    {
        private readonly IWorkoutRepository _workoutRepository;
        private readonly IMapper _mapper;

        public WorkoutService(IWorkoutRepository workoutRepository, IMapper mapper)
        {
            _workoutRepository = workoutRepository;
            _mapper = mapper;
        }

        public async Task<List<string>?> GetCategories()
        {
            return await _workoutRepository.GetCategories();
        }

        public async Task<List<ExerciseDto>> GetExercisesAsync(string? category, string? equipment, string? search)
        {
            List<Exercise> exercises = await _workoutRepository.GetExercises(category, equipment, search);
            return _mapper.Map<List<ExerciseDto>>(exercises);
        }

        public async Task<List<ExerciseDto>> GetExercisesByCategoryAsync(string category)
        {
            List<Exercise> exercises = await _workoutRepository.GetExercisesByCategory(category);
            return _mapper.Map<List<ExerciseDto>>(exercises);
        }

        public async Task<ExerciseDto?> GetExerciseBySlugAsync(string slug)
        {
            Exercise? exercise = await _workoutRepository.GetExerciseBySlug(slug);
            return exercise == null ? null : _mapper.Map<ExerciseDto>(exercise);
        }
    }
}
