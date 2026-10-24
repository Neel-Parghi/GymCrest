namespace GymCrest.Contracts.Gym.Dashboard
{
    public class GymOwnerDashboardDto
    {
        public int TotalMembers { get; set; }
        public double MemberGrowthPercentage { get; set; }
        public int ActiveMembers { get; set; }
        public int FrozenMembers { get; set; }
        public int TodayAttendance { get; set; }
        public decimal MonthlyRevenue { get; set; }
        public decimal MembershipRevenue { get; set; }
        public decimal ProductSalesRevenue { get; set; }
        public int LowStockItems { get; set; }
        public int TotalProductsCount { get; set; }
        public int BranchesCount { get; set; }
        public int ActiveTrainers { get; set; }
        public int SupportStaffCount { get; set; }
        public double RevenueTrendPercentage { get; set; }

        public List<RecentEnrollmentDto> RecentEnrollments { get; set; } = new();
        public List<UpcomingRenewalDto> UpcomingRenewals { get; set; } = new();
        public List<HourlyOccupancyDto> HourlyOccupancy { get; set; } = new();
        public List<WeeklyOccupancyDto> WeeklyOccupancy { get; set; } = new();
        public List<MembershipDistributionDto> MembershipDistribution { get; set; } = new();
        public List<string> TodayCheckedInInitials { get; set; } = new();
    }

    public class HourlyOccupancyDto
    {
        public string Hour { get; set; } = string.Empty;
        public int OccupancyCount { get; set; }
    }

    public class MembershipDistributionDto
    {
        public string TierName { get; set; } = string.Empty;
        public int Count { get; set; }
        public double Percentage { get; set; }
        public string Color { get; set; } = string.Empty;
    }

    public class RecentEnrollmentDto
    {
        public string MemberName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PlanName { get; set; } = string.Empty;
        public DateTime EnrollmentDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Initials { get; set; } = string.Empty;
    }

    public class UpcomingRenewalDto
    {
        public string MemberName { get; set; } = string.Empty;
        public int DaysRemaining { get; set; }
        public DateTime EndDate { get; set; }
    }

    public class WeeklyOccupancyDto
    {
        public string Day { get; set; } = string.Empty;
        public int OccupancyCount { get; set; }
    }
}
