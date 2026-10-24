using System;
using System.Collections.Generic;

namespace GymCrest.Contracts.Members
{
    public class MemberDashboardResponse
    {
        public List<AtRiskMemberDto> AtRiskMembers { get; set; } = new();
        public RenewalFunnelDto RenewalFunnel { get; set; } = new();
        public List<StreakLeaderDto> StreakLeaderboard { get; set; } = new();
        public int TotalCount { get; set; }
        public int ActiveCount { get; set; }
        public int FrozenCount { get; set; }
        public int ExpiredCount { get; set; }
    }

    public class AtRiskMemberDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Plan { get; set; } = string.Empty;
        public string LastActive { get; set; } = string.Empty;
        public string Initials { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty; // critical, danger, warning
    }

    public class RenewalFunnelDto
    {
        public int Expired { get; set; }
        public int Reminded { get; set; }
        public int Renewed { get; set; }
        public decimal ConversionRate { get; set; }
    }

    public class StreakLeaderDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Streak { get; set; } = string.Empty;
        public string Plan { get; set; } = string.Empty;
        public int Rank { get; set; }
        public string Initials { get; set; } = string.Empty;
    }
}
