using System;

namespace GymCrest.Contracts.Gym.Shared
{
    public class BranchDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string? ContactNumber { get; set; }
        public string? OpenTime { get; set; }
        public string? CloseTime { get; set; }
        public string? ManagerName { get; set; }
        public Guid? ManagerId { get; set; }

        public AddressDto Address { get; set; } = null!;
    }
}
