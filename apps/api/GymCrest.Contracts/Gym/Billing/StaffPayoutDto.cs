using System;

namespace GymCrest.Contracts.Gym.Billing
{
    public class StaffPayoutDto
    {
        public Guid StaffId { get; set; }
        public string Id { get; set; } = string.Empty;
        public string StaffName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Initials { get; set; } = string.Empty;
        public decimal BaseSalary { get; set; }
        public decimal Commissions { get; set; }
        public decimal TotalPayout { get; set; }
        public string Status { get; set; } = "Pending";
        public decimal PTCommissionRate { get; set; }
        public decimal RehabCommissionRate { get; set; }
        public DateTime? PayoutDate { get; set; }
    }
}
