using GymCrest.Domain.Interface;
using System.ComponentModel.DataAnnotations.Schema;

namespace GymCrest.Domain.Entities
{
    public class UserNotification : BaseEntity, IBranchScoped
    {
        public Guid GymId { get; set; }
        public Guid? BranchId { get; set; }
        public Guid UserId { get; set; }

        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;

        public bool IsRead { get; set; } = false;

        [ForeignKey(nameof(GymId))]
        public Gym Gym { get; set; } = null!;

        [ForeignKey(nameof(BranchId))]
        public Branch? Branch { get; set; }

        [ForeignKey(nameof(UserId))]
        public User User { get; set; } = null!;
    }
}
