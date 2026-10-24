using System;
using System.Collections.Generic;

namespace GymCrest.Contracts.SuperAdmin
{
    public class SuperAdminDashboardDto
    {
        public PlatformHealthDto? Health { get; set; }
        public MetricDto? TotalRevenue { get; set; }
        public MetricDto? Subscriptions { get; set; }
        public MetricDto? TotalGyms { get; set; }
        public List<RecentGymRegistrationDto>? RecentRegistrations { get; set; }
    }

    public class PlatformHealthDto
    {
        public string? Status { get; set; } 
        public string? LastCheck { get; set; }
    }

    public class MetricDto
    {
        public string? Value { get; set; }
        public string? Trend { get; set; }
        public string? Subtext { get; set; }
        public int? Progress { get; set; } 
    }

    public class RecentGymRegistrationDto
    {
        public Guid? Id { get; set; }
        public string? GymName { get; set; }
        public string? OwnerName { get; set; }
        public DateTime? DateJoined { get; set; }
        public string? Tier { get; set; }
        public string? Status { get; set; }
        public string? Color { get; set; } 
        public string? Initials { get; set; }
    }
}
