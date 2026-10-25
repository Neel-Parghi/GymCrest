namespace GymCrest.Contracts.Feedback
{
    public class CreateFeedbackRequest
    {
        public string Category { get; set; } = "Other";
        public string Message { get; set; } = string.Empty;
        public string? PageUrl { get; set; }
    }

    public class FeedbackDto
    {
        public Guid Id { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? PageUrl { get; set; }
        public string? UserAgent { get; set; }
        public string? UserEmail { get; set; }
        public string? UserRole { get; set; }
        public bool IsResolved { get; set; }
        public DateTime CreatedOn { get; set; }
    }
}
