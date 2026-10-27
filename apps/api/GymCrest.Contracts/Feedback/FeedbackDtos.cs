using System.ComponentModel.DataAnnotations;
namespace GymCrest.Contracts.Feedback
{
    public class CreateFeedbackRequest
    {
        public string Category { get; set; } = "Other";
        [MaxLength(2000)]
        public string Message { get; set; } = string.Empty;
        [MaxLength(2000)]
        public string? PageUrl { get; set; }
    }

    public class FeedbackDto
    {
        public Guid Id { get; set; }
        public string Category { get; set; } = string.Empty;
        [MaxLength(2000)]
        public string Message { get; set; } = string.Empty;
        [MaxLength(2000)]
        public string? PageUrl { get; set; }
        [MaxLength(1000)]
        public string? UserAgent { get; set; }
        public string? UserEmail { get; set; }
        public string? UserRole { get; set; }
        public bool IsResolved { get; set; }
        public DateTime CreatedOn { get; set; }
    }
}
