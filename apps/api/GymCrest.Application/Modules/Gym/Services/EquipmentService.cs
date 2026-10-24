using AutoMapper;
using GymCrest.Application.Modules.Gym.Interfaces;
using GymCrest.Contracts.Common;
using GymCrest.Contracts.Gym.Inventory;
using GymCrest.Domain.Entities;
using GymCrest.Domain.Interface;
using GymCrest.Shared.Models;

namespace GymCrest.Application.Modules.Gym.Services
{
    public class EquipmentService : IEquipmentService
    {
        private readonly IEquipmentRepository _equipmentRepository;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;

        public EquipmentService(IEquipmentRepository equipmentRepository, IMapper mapper, IUnitOfWork unitOfWork)
        {
            _equipmentRepository = equipmentRepository;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }

        public async Task<PagedResponse<EquipmentDto>> GetEquipmentAsync(Guid gymId, PaginationParams pagination, Guid? branchId = null)
        {
            (IEnumerable<Equipment>? items, int totalCount) = await _equipmentRepository.GetPagedEquipmentAsync(
                gymId, 
                pagination.PageNumber, 
                pagination.PageSize, 
                pagination.SearchTerm, 
                branchId);

            List<EquipmentDto> dtos = _mapper.Map<List<EquipmentDto>>(items);
            return new PagedResponse<EquipmentDto>(dtos, totalCount, pagination.PageNumber, pagination.PageSize);
        }

        public async Task<EquipmentDto> AddEquipmentAsync(CreateEquipmentDto dto, Guid gymId)
        {
            Equipment equipment = _mapper.Map<Equipment>(dto);
            equipment.GymId = gymId;
            equipment.CreatedOn = DateTime.UtcNow;

            await _equipmentRepository.AddEquipmentAsync(equipment);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<EquipmentDto>(equipment);
        }

        public async Task<bool> UpdateEquipmentAsync(Guid id, CreateEquipmentDto dto)
        {
            Equipment? equipment = await _equipmentRepository.GetEquipmentByIdAsync(id);
            if (equipment == null) return false;

            _mapper.Map(dto, equipment);
            equipment.ModifiedOn = DateTime.UtcNow;

            _equipmentRepository.UpdateEquipment(equipment);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }
    }
}
