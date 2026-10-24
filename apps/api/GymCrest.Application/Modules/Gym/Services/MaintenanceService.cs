using AutoMapper;
using GymCrest.Contracts.Gym.Inventory;
using GymCrest.Application.Modules.Gym.Interfaces;
using GymCrest.Domain.Entities;
using GymCrest.Domain.Interface;
using GymCrest.Contracts.Common;
using GymCrest.Shared.Models;

namespace GymCrest.Application.Modules.Gym.Services
{
    public class MaintenanceService : IMaintenanceService
    {
        private readonly IMaintenanceRepository _maintenanceRepository;
        private readonly IEquipmentRepository _equipmentRepository;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;

        public MaintenanceService(
            IMaintenanceRepository maintenanceRepository, 
            IEquipmentRepository equipmentRepository,
            IMapper mapper, 
            IUnitOfWork unitOfWork)
        {
            _maintenanceRepository = maintenanceRepository;
            _equipmentRepository = equipmentRepository;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> LogMaintenanceAsync(LogMaintenanceDto dto)
        {
            MaintenanceLog? log;
            
            if (dto.Id.HasValue)
            {
                log = await _maintenanceRepository.GetMaintenanceLogByIdAsync(dto.Id.Value);
                if (log == null) return false;
                _mapper.Map(dto, log);
                _maintenanceRepository.UpdateMaintenanceLog(log);
            }
            else
            {
                log = _mapper.Map<MaintenanceLog>(dto);
                log.CreatedOn = DateTime.UtcNow;
                await _maintenanceRepository.AddMaintenanceLogAsync(log);
            }

            Equipment? equipment = await _equipmentRepository.GetEquipmentByIdAsync(dto.EquipmentId);
            if (equipment != null)
            {
                if (dto.Status == "Completed")
                {
                    equipment.HealthPercentage = 100;
                    equipment.LastServiceDate = dto.CompletedDate ?? DateTime.UtcNow;
                    equipment.CurrentCondition = "Excellent";
                    equipment.IsInMaintenance = false;
                }
                else if (dto.Status == "In Progress" || dto.Status == "Scheduled")
                {
                    equipment.IsInMaintenance = true;
                }
                
                _equipmentRepository.UpdateEquipment(equipment);
            }

            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<List<MaintenanceLogDto>> GetMaintenanceHistoryAsync(Guid equipmentId)
        {
            List<MaintenanceLog> logs = await _maintenanceRepository.GetMaintenanceLogsByEquipmentIdAsync(equipmentId);
            return _mapper.Map<List<MaintenanceLogDto>>(logs);
        }

        public async Task<PagedResponse<MaintenanceLogDto>> GetAllMaintenanceLogsAsync(Guid gymId, PaginationParams pagination, Guid? branchId = null)
        {
            (IEnumerable<MaintenanceLog>? items, int totalCount) = await _maintenanceRepository.GetPagedMaintenanceLogsAsync(
                gymId,
                pagination.PageNumber,
                pagination.PageSize,
                pagination.SearchTerm,
                branchId);

            List<MaintenanceLogDto> dtos = _mapper.Map<List<MaintenanceLogDto>>(items);
            return new PagedResponse<MaintenanceLogDto>(dtos, totalCount, pagination.PageNumber, pagination.PageSize);
        }
    }
}
