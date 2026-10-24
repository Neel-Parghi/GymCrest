using System;

namespace GymCrest.Contracts.Staff
{
    public class StaffAttendanceLogResponse
    {
        public Guid Id { get; set; }
        public Guid StaffId { get; set; }
        public string StaffName { get; set; } = string.Empty;
        public string StaffNumber { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public DateTime CheckInTime { get; set; }
        public DateTime? CheckOutTime { get; set; }
        public string? Notes { get; set; }
        public double? HoursWorked { get; set; }
    }
}
