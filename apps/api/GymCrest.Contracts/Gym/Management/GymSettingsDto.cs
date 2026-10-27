using System.ComponentModel.DataAnnotations;
using System;

namespace GymCrest.Contracts.Gym.Management
{
    public class GymSettingsDto
    {
        [MaxLength(4000)]
        public string? RoleRightsMatrixJson { get; set; }

        public int PlanExpirationTriggerDays { get; set; } = 7;
    }
}
