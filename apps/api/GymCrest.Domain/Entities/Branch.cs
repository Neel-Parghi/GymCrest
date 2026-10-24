using System;
using System.Collections.Generic;
using System.Text;

namespace GymCrest.Domain.Entities
{
    public class Branch : BaseEntity
    {
        public Guid GymId { get; set; }
        
        public string Name { get; set; } = string.Empty;

        public Guid AddressId { get; set; }

        public string? ContactNumber { get; set; }

        public bool IsMainBranch { get; set; }
        
        public bool IsActive { get; set; }
        
        public string? OpenTime { get; set; }
        public string? CloseTime { get; set; }

        // Navigation
        public Gym Gym { get; set; } = null!;
        public Address Address { get; set; } = null!;
    }
}
