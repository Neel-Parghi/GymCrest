using System;
using System.ComponentModel.DataAnnotations.Schema;
using GymCrest.Domain.Interface;

namespace GymCrest.Domain.Entities
{
    public class GymAnnouncement : BaseEntity, IBranchScoped
    {
        public Guid GymId { get; set; }
        public Guid? BranchId { get; set; }

        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
        public DateTime? ValidUntil { get; set; }

        [ForeignKey(nameof(GymId))]
        public Gym Gym { get; set; } = null!;

        [ForeignKey(nameof(BranchId))]
        public Branch? Branch { get; set; }
    }
}
