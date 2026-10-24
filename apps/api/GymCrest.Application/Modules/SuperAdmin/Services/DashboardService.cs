using GymCrest.Application.Modules.SuperAdmin.Interfaces;
using GymCrest.Contracts.SaaSPayments;
using GymCrest.Contracts.SuperAdmin.Dashboard;
using GymCrest.Domain.Entities;
using GymCrest.Domain.Interface;

namespace GymCrest.Application.Modules.SuperAdmin.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly IDashboardRepository _dashboardRepository;
        private readonly ISaaSConfigurationRepository _configRepository;
        private readonly Payments.Interfaces.ISaaSPaymentService _saasPaymentService;

        public DashboardService(
            IDashboardRepository dashboardRepository, 
            ISaaSConfigurationRepository configRepository,
            Payments.Interfaces.ISaaSPaymentService saasPaymentService)
        {
            _dashboardRepository = dashboardRepository;
            _configRepository = configRepository;
            _saasPaymentService = saasPaymentService;
        }

        public async Task<SuperAdminDashboardDto> GetDashboardStatsAsync()
        {
            DateTime now = DateTime.UtcNow;
            DateTime thisMonthStart = new(now.Year, now.Month, 1);

            PaymentStatsDto paymentStats = await _saasPaymentService.GetPaymentStatsAsync();
            decimal totalRevenue = paymentStats.TotalRevenue;
            decimal mrr = paymentStats.MonthlyRecurringRevenue;
            decimal arr = mrr * 12;

            decimal prevMonthRevenue = await _dashboardRepository.GetTotalRevenueAsync(null, thisMonthStart);
            
            int activeSubs = paymentStats.ActiveSubscriptions;
            int prevActiveSubs = await _dashboardRepository.GetActiveSubscriptionsCountAsync(thisMonthStart);

            int totalGyms = await _dashboardRepository.GetTotalGymsCountAsync();
            int pendingVerifications = await _dashboardRepository.GetPendingVerificationsCountAsync();
            List<PlanDistributionDto> planDistribution = await _dashboardRepository.GetPlanDistributionAsync();
            List<RecentGymRegistrationDto> recentRegistrations = await _dashboardRepository.GetRecentGymRegistrationsAsync(10);

            SaaSConfiguration config = await _configRepository.GetConfigurationAsync();
            
            int revenueProgress = 0;
            if (config.YearlyRevenueTarget > 0)
            {
                revenueProgress = (int)Math.Min(100, (arr / config.YearlyRevenueTarget) * 100);
            }

            int subscriptionProgress = 0;
            if (config.SubscriptionTarget > 0)
            {
                subscriptionProgress = Math.Min(100, (activeSubs * 100) / config.SubscriptionTarget);
            }

            return new SuperAdminDashboardDto
            {
                Health = new PlatformHealthDto
                {
                    PendingVerifications = pendingVerifications,
                    Status = pendingVerifications > 0 ? "ACTION REQUIRED" : "ALL CLEAR",
                    LastCheck = DateTime.UtcNow.ToString("h:mm tt") + " UTC"
                },
                TotalRevenue = new MetricDto
                {
                    CurrentValue = totalRevenue,
                    PreviousValue = prevMonthRevenue,
                    Progress = revenueProgress
                },
                MonthlyRecurringRevenue = new MetricDto
                {
                    CurrentValue = mrr,
                    PreviousValue = 0,
                    Progress = revenueProgress
                },
                AnnualRecurringRevenue = new MetricDto
                {
                    CurrentValue = arr,
                    PreviousValue = 0,
                    Progress = revenueProgress
                },
                Subscriptions = new MetricDto
                {
                    CurrentValue = activeSubs,
                    PreviousValue = prevActiveSubs,
                    Progress = subscriptionProgress
                },
                TotalGyms = new MetricDto
                {
                    CurrentValue = totalGyms,
                    PreviousValue = totalGyms - recentRegistrations.Count
                },
                PlanDistribution = planDistribution,
                RecentRegistrations = recentRegistrations
            };
        }
    }
}
