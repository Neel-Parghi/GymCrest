using GymCrest.Contracts.Common;
using GymCrest.Contracts.Gym.Management;
using GymCrest.Contracts.Gym.Owners;
using GymCrest.Contracts.Gym.Onboarding;
using GymCrest.Contracts.Gym.Shared;
using GymCrest.Shared.Models;

namespace GymCrest.Application.Modules.Gym.Interfaces
{
    public interface IGymManagementService
    {
        Task<Guid> OnboardGymAsync(Guid ownerId, GymOnboardingDto gymOnboardingDto, bool activateImmediately);

        Task<PagedResponse<GymOwnersDto>> GetGymOwnersList(GymOwnerFilterParams filters);
        Task<GymOwnersDto> UpdateGymOwner(UpdateGymOwnerDto updateGymOwnerDto);

        Task<PagedResponse<GymListResponseDto>> GetGymListAsync(GymFilterParams filters);
        Task UpdateGymAsync(UpdateGymDto updateGymDto);
        Task<bool> DeleteGymAsync(Guid gymId);
        Task<bool> DeleteGymOwnerAsync(Guid ownerId);
        Task AddBranchAsync(Guid gymId, BranchDto branchDto);
        Task<List<BranchDto>> GetBranchesByGymIdAsync(Guid gymId);
        Task UpdateBranchAsync(Guid branchId, BranchDto branchDto);
        Task<GymListResponseDto?> GetGymByOwnerIdAsync(Guid ownerId);
        Task<GymListResponseDto?> GetGymDetailsByIdAsync(Guid gymId);
        Task UpdateMyGymAsync(Guid ownerId, UpdateMyGymDto updateMyGymDto);
        Task<GymSettingsDto> GetGymSettingsAsync(Guid gymId);
        Task UpdateGymSettingsAsync(Guid gymId, GymSettingsDto dto);
        Task<List<GymHolidayDto>> GetHolidaysAsync(Guid gymId);
        Task AddHolidayAsync(Guid gymId, GymHolidayDto dto);
        Task DeleteHolidayAsync(Guid gymId, Guid holidayId);
    }
}
