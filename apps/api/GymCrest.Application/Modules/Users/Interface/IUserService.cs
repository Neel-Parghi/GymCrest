using GymCrest.Contracts.Common;
using GymCrest.Contracts.PlanGeneration;
using GymCrest.Contracts.Users;
using GymCrest.Shared.Models;
using Microsoft.AspNetCore.Http;

namespace GymCrest.Application.Modules.Users.Interface
{
    public interface IUserService
    {
        Task InviteOwnerAsync(InviteOwnerRequestDto inviteOwnerRequestDto);

        Task SetPasswordAsync(SetPasswordRequestDto setPasswordRequestDto);

        Task ChangePasswordAsync(Guid userId, ChangePasswordRequestDto dto);

        Task<bool> ValidateInvitationTokenAsync(string token);

        Task ReInviteOwnerAsync(Guid userId);

        Task<UserProfileDto> GetUserProfileAsync(Guid userId);

        Task<Guid?> GetUserGymIdAsync(Guid userId);

        Task UpdateUserProfileAsync(Guid userId, UpdateUserProfileDto updateUserProfileDto);

        Task<UserProfileDto> GetMyProfileAsync();

        Task UpdateMyProfileAsync(UpdateUserProfileDto dto);

        Task<UserPreferenceDto> GetMyPreferencesAsync();
        
        Task UpdateMyPreferencesAsync(UpdateUserPreferenceDto dto);
        
        Task ChangeMyPasswordAsync(ChangePasswordRequestDto dto);

        Task<string> UploadAvatarAsync(IFormFile file);

        Task<IEnumerable<DeletionRequestDto>> GetPendingDeletionRequestsAsync();

        Task<PagedResponse<StandaloneUserDto>> GetStandaloneUsersAsync(PaginationParams pagination);


        Task DeleteUserAsAdminAsync(Guid userId);

        Task SaveOnboardingStepAsync(Guid userId, int step);

        Task CompleteUserOnboardingAsync(Guid userId, CompleteUserOnboardingDto dto);

        Task<StarterPlanResultDto?> GetSuggestedTargetsAsync(string? goal, string? activityLevel);

        StarterPlanResultDto PreviewTargets(StarterPlanProfileDto profile);

        Task<StarterPlanResultDto> RegenerateStarterPlansAsync();

        Task<UserDashboardSummaryDto> GetUserDashboardSummaryAsync();
        
        Task<IEnumerable<DailyRoutineDto>> GetDailyRoutinesAsync();
        
        Task<DailyRoutineDto> CreateDailyRoutineAsync(CreateDailyRoutineDto dto);
        
        Task UpdateDailyRoutineAsync(Guid id, UpdateDailyRoutineDto dto);
        
        Task DeleteDailyRoutineAsync(Guid id);
        
        Task ToggleDailyRoutineAsync(Guid id);
    }
}
