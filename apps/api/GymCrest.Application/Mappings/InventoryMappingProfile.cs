using AutoMapper;
using GymCrest.Contracts.Gym.Inventory;
using GymCrest.Domain.Entities;

namespace GymCrest.Application.Mappings
{
    public class InventoryMappingProfile : Profile
    {
        public InventoryMappingProfile()
        {
            // Products
            CreateMap<InventoryItem, InventoryItemDto>();
            CreateMap<CreateProductDto, InventoryItem>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.GymId, opt => opt.Ignore());

            // Equipment
            CreateMap<Equipment, EquipmentDto>()
                .ForMember(dest => dest.Health, opt => opt.MapFrom(src => src.HealthPercentage))
                .ForMember(dest => dest.Condition, opt => opt.MapFrom(src => src.CurrentCondition))
                .ForMember(dest => dest.LastService, opt => opt.MapFrom(src => src.LastServiceDate));
            
            CreateMap<CreateEquipmentDto, Equipment>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.GymId, opt => opt.Ignore())
                .ForMember(dest => dest.HealthPercentage, opt => opt.MapFrom(src => src.InitialHealth))
                .ForMember(dest => dest.MaintenanceIntervalMonths, opt => opt.MapFrom(src => src.MaintenanceInterval))
                .ForMember(dest => dest.CurrentCondition, opt => opt.MapFrom(src => src.Condition));

            // Sales
            CreateMap<SaleTransaction, SaleTransactionDto>()
                .ForMember(dest => dest.MemberName, opt => opt.MapFrom(src => $"{src.Member.FirstName} {src.Member.LastName}"))
                .ForMember(dest => dest.MemberId, opt => opt.MapFrom(src => src.Member.MembershipNumber))
                .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.InventoryItem.Name))
                .ForMember(dest => dest.Date, opt => opt.MapFrom(src => src.TransactionDate));

            // Maintenance
            CreateMap<LogMaintenanceDto, MaintenanceLog>();
            CreateMap<MaintenanceLog, MaintenanceLogDto>()
                .ForMember(dest => dest.EquipmentName, opt => opt.MapFrom(src => src.Equipment.Name));
        }
    }
}
