using System;

namespace GymCrest.Contracts.Gym.Billing
{
    public class UpdateStaffPayrollRuleRequest
    {
        public Guid StaffId { get; set; }
        public decimal BaseSalary { get; set; }
        public decimal PTCommissionRate { get; set; }
        public decimal RehabCommissionRate { get; set; }
    }
}
