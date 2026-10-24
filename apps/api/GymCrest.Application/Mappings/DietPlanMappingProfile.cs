using AutoMapper;
using GymCrest.Contracts.DietPlan;
using GymCrest.Domain.Entities;

namespace GymCrest.Application.Mappings
{
    public class DietPlanMappingProfile : Profile
    {
        public DietPlanMappingProfile()
        {
            // Response Mappings
            CreateMap<DietPlan, DietPlanDto>();
            CreateMap<DietPlanMeal, DietPlanMealDto>();

            // Request Mappings
            CreateMap<CreateDietPlanRequest, DietPlan>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedOn, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.ModifiedOn, opt => opt.Ignore())
                .ForMember(dest => dest.ModifiedBy, opt => opt.Ignore())
                .ForMember(dest => dest.Calories, opt => opt.Ignore());
            CreateMap<CreateDietPlanMealDto, DietPlanMeal>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.DietPlanId, opt => opt.Ignore())
                .ForMember(dest => dest.DietPlan, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedOn, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.ModifiedOn, opt => opt.Ignore())
                .ForMember(dest => dest.ModifiedBy, opt => opt.Ignore());

            CreateMap<UpdateDietPlanRequest, DietPlan>()
                .ForMember(dest => dest.CreatedOn, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.ModifiedOn, opt => opt.Ignore())
                .ForMember(dest => dest.ModifiedBy, opt => opt.Ignore())
                .ForMember(dest => dest.Calories, opt => opt.Ignore());
        }
    }
}
