using AutoMapper;
using GymCrest.Contracts.Auth;
using GymCrest.Contracts.Gym.Owners;
using GymCrest.Domain.Entities;

namespace GymCrest.Application.Mappings
{
    public class UserMappingProfile : Profile
    {
        public UserMappingProfile() 
        {
            CreateMap<RegisterRequestDto, User>();

            CreateMap<User, AuthResponseDto>();

            CreateMap<UpdateGymOwnerDto, User>();

            CreateMap<User, GymOwnersDto>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}"));

            CreateMap<User, GymCrest.Contracts.Users.UserProfileDto>();

            CreateMap<GymCrest.Contracts.Users.UpdateUserProfileDto, Address>()
                .ForMember(dest => dest.Address1, opt => opt.MapFrom(src => src.AddressLine1))
                .ForMember(dest => dest.Address2, opt => opt.MapFrom(src => src.AddressLine2))
                .ForMember(dest => dest.PostalCode, opt => opt.MapFrom(src => src.ZipCode));
        }
    }
}
