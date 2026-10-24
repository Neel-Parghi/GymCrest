using GymCrest.Contracts.Gym.Inventory;
using GymCrest.Contracts.Common;
using GymCrest.Shared.Models;

namespace GymCrest.Application.Modules.Gym.Interfaces
{
    public interface IEquipmentService
    {
        Task<PagedResponse<EquipmentDto>> GetEquipmentAsync(Guid gymId, PaginationParams pagination, Guid? branchId = null);
        Task<EquipmentDto> AddEquipmentAsync(CreateEquipmentDto dto, Guid gymId);
        Task<bool> UpdateEquipmentAsync(Guid id, CreateEquipmentDto dto);
    }
}
