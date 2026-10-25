namespace GymCrest.Domain.Entities
{
    /// <summary>
    /// A bug report, idea or comment sent from the app. Who sent it is copied in (not a foreign key) so the
    /// report survives if the account is deleted.
    /// </summary>
    public class Feedback : BaseEntity
    {
        public Guid? UserId { get; set; }
        public string? UserEmail { get; set; }
        public string? UserRole { get; set; }
        public Guid? GymId { get; set; }

        public string Category { get; set; } = "Other";
        public string Message { get; set; } = string.Empty;

        public string? PageUrl { get; set; }
        public string? UserAgent { get; set; }

        public bool IsResolved { get; set; }
    }
}
