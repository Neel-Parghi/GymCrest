namespace GymCrest.Contracts.Attendance
{
    public class AttendanceLogResponse
    {
        public Guid Id { get; set; }
        public Guid MemberId { get; set; }
        public string MemberName { get; set; } = string.Empty;
        public string MembershipNumber { get; set; } = string.Empty;
        public string Initials { get; set; } = string.Empty;
        public string PlanName { get; set; } = string.Empty;
        public DateTime CheckInTime { get; set; }
        public DateTime? CheckOutTime { get; set; }
        public string Duration { get; set; } = "--";
        public string Status { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
    }

    public class CheckedInMemberResponse
    {
        public Guid LogId { get; set; }
        public Guid MemberId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string MembershipNumber { get; set; } = string.Empty;
        public string Initials { get; set; } = string.Empty;
        public string PlanName { get; set; } = string.Empty;
        public DateTime CheckInTime { get; set; }
    }

    public class OccupancyStatsResponse
    {
        public int CurrentHeadcount { get; set; }
        public int SafetyLimit { get; set; }
        public double UtilizationPercentage { get; set; }
    }
}
