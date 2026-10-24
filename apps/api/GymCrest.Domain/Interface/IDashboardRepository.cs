using GymCrest.Contracts.SuperAdmin.Dashboard;

namespace GymCrest.Domain.Interface
{
    public interface IDashboardRepository
    {
        Task<decimal> GetTotalRevenueAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<int> GetActiveSubscriptionsCountAsync(DateTime? asOfDate = null);
        Task<int> GetTotalGymsCountAsync();
        Task<int> GetPendingVerificationsCountAsync();
        Task<List<PlanDistributionDto>> GetPlanDistributionAsync();
        Task<List<RecentGymRegistrationDto>> GetRecentGymRegistrationsAsync(int count);
    }
}
