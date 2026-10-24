using GymCrest.Contracts.Common;
using GymCrest.Contracts.Members;
using GymCrest.Shared.Models;

namespace GymCrest.Application.Modules.Gym.Interfaces
{
    public interface IGymMemberService
    {
        Task<GymMemberResponse> OnboardMemberAsync(Guid gymId, OnboardMemberRequest request, Guid createdBy);
        Task<PagedResponse<GymMemberResponse>> GetGymMembersAsync(Guid gymId, MemberFilterParams filter, Guid? branchId = null);
        Task<GymMemberResponse?> GetMemberByIdAsync(Guid id);
        Task<GymMemberResponse> UpdateMemberAsync(Guid id, OnboardMemberRequest request, Guid updatedBy);
        Task<bool> ToggleMemberStatusAsync(Guid id);
        Task<bool> FreezeMemberAsync(Guid id, Guid updatedBy, DateTime freezeUntil);
        Task<bool> UnfreezeMemberAsync(Guid id, Guid updatedBy);
        Task<GymMemberResponse> RenewSubscriptionAsync(Guid memberId, RenewSubscriptionRequest request, Guid updatedBy);
        Task<bool> DeleteMemberAsync(Guid id);
        Task<IEnumerable<MemberSubscriptionResponse>> GetSubscriptionHistoryAsync(Guid memberId);
        Task<IEnumerable<MemberSubscriptionResponse>> GetSubscriptionHistoryByUserIdAsync(Guid userId);
        Task<byte[]> ExportMembersAsync(Guid gymId, Guid? branchId = null);
        Task<MemberDashboardResponse> GetMemberDashboardDataAsync(Guid gymId, Guid? branchId = null);
        Task<MyGymMembershipResponse?> GetMyGymMembershipAsync(Guid userId);
    }
}
