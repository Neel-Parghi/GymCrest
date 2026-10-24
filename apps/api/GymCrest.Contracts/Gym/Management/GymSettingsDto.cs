using System;

namespace GymCrest.Contracts.Gym.Management
{
    public class GymSettingsDto
    {
        public string? RoleRightsMatrixJson { get; set; }

        public int PlanExpirationTriggerDays { get; set; } = 7;
    }
}
