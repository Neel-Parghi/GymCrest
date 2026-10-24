using AutoMapper;
using GymCrest.Contracts.Workout;
using GymCrest.Domain.Entities;

namespace GymCrest.Application.Mappings
{
    public class WorkoutMappingProfile : Profile
    {
        public WorkoutMappingProfile()
        {
            CreateMap<Exercise, ExerciseDto>();
        }
    }
}
