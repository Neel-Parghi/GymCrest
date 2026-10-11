using GymForge.Domain.Entities;
using GymForge.Contracts.Members;

namespace GymForge.Domain.Interface
{
    public interface IGymMemberRepository
    {
        Task<GymMember?> GetByIdAsync(Guid id); 
        Task<GymMember?> GetByUserIdAsync(Guid userId);
        Task<List<GymMember>> GetAllByUserIdAsync(Guid userId);
        Task<IEnumerable<GymMember>> GetAllByGymIdAsync(Guid gymId, Guid? branchId = null); 
        Task<(IEnumerable<GymMember> Items, int TotalCount)> GetPagedMembersAsync(Guid gymId, MemberFilterParams filter, Guid? branchId = null);
        Task<bool> ExistsByEmailAsync(string email, Guid gymId);
        Task AddAsync(GymMember member); 
        Task AddSubscriptionAsync(MemberSubscription subscription);
        Task UpdateAsync(GymMember member);
        Task DeactivateActiveSubscriptionsAsync(Guid memberId);
        Task DeleteAsync(Guid id);
        Task<IEnumerable<MemberSubscription>> GetActiveSubscriptionsByGymIdAsync(Guid gymId);
        Task<MemberDashboardResponse> GetMemberDashboardDataAsync(Guid gymId, Guid? branchId = null);
        Task<IEnumerable<GymMember>> GetMembersDueForUnfreezeAsync(DateTime asOf);
    }
}
