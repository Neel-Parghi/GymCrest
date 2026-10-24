using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace GymCrest.Domain.Entities
{
    public class GymHoliday : BaseEntity
    {
        public Guid GymId { get; set; }
        
        public Guid? BranchId { get; set; }

        public string Name { get; set; } = string.Empty;

        public DateTime Date { get; set; }

        [ForeignKey(nameof(GymId))]
        public Gym Gym { get; set; } = null!;

        [ForeignKey(nameof(BranchId))]
        public Branch? Branch { get; set; }
    }
}
