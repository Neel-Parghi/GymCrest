using System;

namespace GymCrest.Contracts.Gym.Billing
{
    public class ReleaseStaffPayoutRequest
    {
        public Guid StaffId { get; set; }
        public string MonthKey { get; set; } = string.Empty;
        public string Status { get; set; } = "Paid";
        public decimal Commissions { get; set; }
        public decimal TotalPayout { get; set; }
    }
}
