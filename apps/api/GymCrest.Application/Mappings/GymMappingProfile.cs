using AutoMapper;
using GymCrest.Contracts.Gym.Management;
using GymCrest.Contracts.Gym.Onboarding;
using GymCrest.Contracts.Gym.Owners;
using GymCrest.Contracts.Gym.Shared;
using GymCrest.Domain.Entities;
using GymCrest.Shared.Enums;

namespace GymCrest.Application.Mappings
{
    public class GymMappingProfile : Profile
    {
        public GymMappingProfile()
        {
            CreateMap<AddressDto, Address>().ReverseMap();
            
            CreateMap<BranchDto, Branch>().ReverseMap();

            CreateMap<UpdateGymDto, Gym>();
            CreateMap<UpdateMyGymDto, Gym>()
                .ForMember(dest => dest.DefaultTaxRate, opt => opt.PreCondition(src => src.DefaultTaxRate.HasValue))
                .ForMember(dest => dest.OverdueGraceDays, opt => opt.PreCondition(src => src.OverdueGraceDays.HasValue))
                .ForMember(dest => dest.AutoEmailReceipts, opt => opt.PreCondition(src => src.AutoEmailReceipts.HasValue))
                .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<OwnerDto, User>()
                .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => 
                    src.FullName.Contains(" ") ? src.FullName.Substring(0, src.FullName.IndexOf(" ")) : src.FullName))
                .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => 
                    src.FullName.Contains(" ") ? src.FullName.Substring(src.FullName.IndexOf(" ") + 1) : string.Empty))
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => UserRole.GymOwner));

            CreateMap<GymOnboardingDto, Gym>()
                .ForMember(dest => dest.GymName, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Address, opt => opt.Ignore())
                .ForMember(dest => dest.Owner, opt => opt.Ignore())
                .ForMember(dest => dest.Branches, opt => opt.Ignore());
        }
    }
}
