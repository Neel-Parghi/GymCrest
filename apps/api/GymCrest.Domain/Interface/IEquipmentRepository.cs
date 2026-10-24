using GymCrest.Domain.Entities;

namespace GymCrest.Domain.Interface
{
    public interface IEquipmentRepository
    {
        Task<Equipment?> GetEquipmentByIdAsync(Guid id);
        Task<List<Equipment>> GetEquipmentByGymIdAsync(Guid gymId, Guid? branchId = null);
        Task<(IEnumerable<Equipment> items, int totalCount)> GetPagedEquipmentAsync(Guid gymId, int pageNumber, int pageSize, string? searchTerm, Guid? branchId = null);
        Task AddEquipmentAsync(Equipment equipment);
        Equipment UpdateEquipment(Equipment equipment);
        Task DeleteEquipmentAsync(Guid id);
    }
}
