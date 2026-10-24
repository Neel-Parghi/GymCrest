using AutoMapper;
using GymCrest.Contracts.WorkoutPlan;
using GymCrest.Domain.Entities;

namespace GymCrest.Application.Mappings
{
    public class WorkoutPlanMappingProfile : Profile
    {
        public WorkoutPlanMappingProfile()
        {
            // Response Mappings
            CreateMap<WorkoutPlan, WorkoutPlanDto>();
            CreateMap<WorkoutPlanDay, WorkoutPlanDayDto>()
                .ForMember(dest => dest.Exercises, opt => opt.MapFrom(src => src.Exercises.OrderBy(e => e.SortOrder)));
            CreateMap<WorkoutPlanExercise, WorkoutPlanExerciseDto>();

            // Request Mappings
            CreateMap<CreateWorkoutPlanRequest, WorkoutPlan>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedOn, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.ModifiedOn, opt => opt.Ignore())
                .ForMember(dest => dest.ModifiedBy, opt => opt.Ignore());

            CreateMap<CreateWorkoutPlanDayDto, WorkoutPlanDay>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.WorkoutPlanId, opt => opt.Ignore())
                .ForMember(dest => dest.WorkoutPlan, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedOn, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.ModifiedOn, opt => opt.Ignore())
                .ForMember(dest => dest.ModifiedBy, opt => opt.Ignore());

            CreateMap<CreateWorkoutPlanExerciseDto, WorkoutPlanExercise>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.WorkoutPlanDayId, opt => opt.Ignore())
                .ForMember(dest => dest.WorkoutPlanDay, opt => opt.Ignore())
                .ForMember(dest => dest.Exercise, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedOn, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.ModifiedOn, opt => opt.Ignore())
                .ForMember(dest => dest.ModifiedBy, opt => opt.Ignore());

            CreateMap<UpdateWorkoutPlanRequest, WorkoutPlan>()
                .ForMember(dest => dest.CreatedOn, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.ModifiedOn, opt => opt.Ignore())
                .ForMember(dest => dest.ModifiedBy, opt => opt.Ignore());

            CreateMap<WorkoutSessionLog, WorkoutSessionLogDto>()
                .ForMember(dest => dest.LoggedExercises, opt => opt.MapFrom(src => src.LoggedExercises.OrderBy(e => e.SortOrder)));
            CreateMap<LoggedExercise, LoggedExerciseDto>()
                .ForMember(dest => dest.LoggedSets, opt => opt.MapFrom(src => src.LoggedSets.OrderBy(s => s.SetNo)));
            CreateMap<LoggedSet, LoggedSetDto>();
            CreateMap<MemberPlanAssignment, MemberPlanAssignmentDto>();
        }
    }
}
