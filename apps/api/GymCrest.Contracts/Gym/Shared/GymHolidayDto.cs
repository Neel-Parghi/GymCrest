using System;

namespace GymCrest.Contracts.Gym.Shared
{
    public class GymHolidayDto
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public DateTime Date { get; set; }

        public Guid? BranchId { get; set; }

        public string? BranchName { get; set; }
    }
}
