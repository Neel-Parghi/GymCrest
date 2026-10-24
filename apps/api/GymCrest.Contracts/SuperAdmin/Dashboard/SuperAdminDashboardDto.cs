using System.Collections.Generic;

namespace GymCrest.Contracts.SuperAdmin.Dashboard
{
    public class SuperAdminDashboardDto
    {
        public PlatformHealthDto? Health { get; set; }
        public MetricDto? TotalRevenue { get; set; }
        public MetricDto? MonthlyRecurringRevenue { get; set; }
        public MetricDto? AnnualRecurringRevenue { get; set; }
        public MetricDto? Subscriptions { get; set; }
        public MetricDto? TotalGyms { get; set; }
        public List<PlanDistributionDto>? PlanDistribution { get; set; }
        public List<RecentGymRegistrationDto>? RecentRegistrations { get; set; }
    }
}
