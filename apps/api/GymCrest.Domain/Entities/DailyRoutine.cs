using System;

namespace GymCrest.Domain.Entities
{
    public class DailyRoutine : BaseEntity
    {
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;
        public string Title { get; set; } = string.Empty;
        public string? Time { get; set; }
        public string? Amount { get; set; }
        public bool IsActive { get; set; } = true;
        public int Order { get; set; } = 0;
    }
}
