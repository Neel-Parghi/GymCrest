using System;

namespace GymCrest.Contracts.SaaSPayments
{
    public class GymSubscriptionStatusDto
    {
        public string PlanName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
        public bool IsTrial { get; set; }
        public int BranchUsageCount { get; set; }
        public int BranchLimit { get; set; }
    }
}
