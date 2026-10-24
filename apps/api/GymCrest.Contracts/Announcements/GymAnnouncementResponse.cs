using System;

namespace GymCrest.Contracts.Announcements
{
    public class GymAnnouncementResponse
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime? ValidUntil { get; set; }
        public DateTime CreatedOn { get; set; }
    }
}
