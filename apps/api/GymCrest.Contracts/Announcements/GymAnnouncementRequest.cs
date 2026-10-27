using System;
using System.ComponentModel.DataAnnotations;

namespace GymCrest.Contracts.Announcements
{
    public class GymAnnouncementRequest
    {
        [Required]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(1000)]
        public string Message { get; set; } = string.Empty;
        
        public bool IsActive { get; set; } = true;
        public DateTime? ValidUntil { get; set; }
    }
}
