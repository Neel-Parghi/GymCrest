using AutoMapper;
using GymCrest.Contracts.GymPlans;
using GymCrest.Domain.Entities;

namespace GymCrest.Application.Mappings
{
    public class GymPlanMappingProfile : Profile
    {
        public GymPlanMappingProfile() 
        {
            CreateMap<CreateGymPlanRequest, GymPlan>()
                .ForMember(dest => dest.Id, opt => opt.Ignore());

            CreateMap<UpdateGymPlanRequest, GymPlan>();

            CreateMap<GymPlan, GymPlanDto>();
        }
    }
}
