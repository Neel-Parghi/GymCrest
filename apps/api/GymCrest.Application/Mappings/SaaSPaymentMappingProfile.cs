using AutoMapper;
using GymCrest.Contracts.SaaSPayments;
using GymCrest.Contracts.SuperAdmin.Configuration;
using GymCrest.Domain.Entities;

namespace GymCrest.Application.Mappings
{
    public class SaaSPaymentMappingProfile : Profile
    {
        public SaaSPaymentMappingProfile()
        {
            CreateMap<SaaSConfiguration, SaaSConfigurationDto>().ReverseMap();
        }
    }
}
