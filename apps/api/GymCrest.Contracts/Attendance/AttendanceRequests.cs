using GymCrest.Contracts.Common;
using System;

namespace GymCrest.Contracts.Attendance
{
    public record CheckInRequest(Guid MemberId, string Method = "Manual");
    public record CheckOutRequest(Guid MemberId);

    public class AttendanceLogQueryParams : PaginationParams
    {
        public string? Status { get; set; }
        public DateTime? Date { get; set; }
    }
}
