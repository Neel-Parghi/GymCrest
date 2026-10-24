using System;

namespace GymCrest.Contracts.SaaSPayments
{
    public class RenewSaaSRequestDto
    {
        public Guid GymId { get; set; }

        public Guid PlanId { get; set; }
    }
}
