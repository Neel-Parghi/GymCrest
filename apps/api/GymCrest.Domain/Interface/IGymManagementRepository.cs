using GymCrest.Contracts.Gym.Management;
using GymCrest.Contracts.Gym.Owners;
using GymCrest.Domain.Entities;

namespace GymCrest.Domain.Interface
{
    public interface IGymManagementRepository
    {
        Task AddGymAsync(Gym gym);
        Task AddBranchAsync(Branch branch);
        Task AddGymSubscriptionAsync(SubscriptionRecord subscription);
       
        Task<(List<GymOwnersDto> Items, int TotalCount)> GetGymOwnersList(int pageNumber, int pageSize, string? searchTerm);
        Task<User?> GetGymOwnerByIdAsync(Guid id);
        User UpdateGymOwner(User gymOwner);
        
        Task<(List<GymListResponseDto> Items, int TotalCount)> GetGymListAsync(int pageNumber, int pageSize, string? searchTerm);
        Task<List<GymListResponseDto>> GetAllGymsAsync();
        Task<Gym?> GetGymByIdAsync(Guid id);
        Gym UpdateGym(Gym gym);
        Task<bool> GymHasUsersAsync(Guid gymId, Guid ownerUserId);
        Task<bool> OwnerHasGymsAsync(Guid ownerId);
        Task DeleteGymOwnerAsync(User owner);
        Task DeleteGymAsync(Guid gymId);
        Task<List<Branch>> GetBranchesByGymIdAsync(Guid gymId);
        Task<Branch?> GetBranchByIdAsync(Guid id);
        Branch UpdateBranch(Branch branch);
        Task<GymListResponseDto?> GetGymByOwnerIdAsync(Guid ownerId);
        Task<GymListResponseDto?> GetGymDetailsResponseByIdAsync(Guid gymId);
        Task<List<Staff>> GetBranchManagersAsync(Guid gymId);
        Task<List<GymHoliday>> GetHolidaysAsync(Guid gymId);
        Task AddHolidayAsync(GymHoliday holiday);
        Task DeleteHolidayAsync(Guid gymId, Guid holidayId);
        Task<GymConfiguration?> GetGymConfigurationAsync(Guid gymId);
    }
}
