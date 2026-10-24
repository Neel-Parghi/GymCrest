using AutoMapper;
using GymCrest.Contracts.Members;
using GymCrest.Domain.Entities;

namespace GymCrest.Application.Mappings
{
    public class GymMemberMappingProfile : Profile
    {
        public GymMemberMappingProfile()
        {
            CreateMap<GymMember, GymMemberResponse>()
                .ForMember(dest => dest.CurrentSubscription, 
                           opt => opt.MapFrom(src => src.Subscriptions
                                                        .OrderByDescending(s => s.CreatedOn)
                                                        .FirstOrDefault(s => s.IsActive)));

            CreateMap<MemberSubscription, MemberSubscriptionResponse>();

            CreateMap<Address, GymCrest.Contracts.Gym.Shared.AddressDto>()
                .ForMember(dest => dest.Line1, opt => opt.MapFrom(src => src.Address1))
                .ForMember(dest => dest.Line2, opt => opt.MapFrom(src => src.Address2))
                .ReverseMap();
            CreateMap<OnboardMemberRequest, GymMember>()
                .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.Address));
        }
    }
}
