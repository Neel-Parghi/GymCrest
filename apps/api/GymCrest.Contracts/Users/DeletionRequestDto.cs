namespace GymCrest.Contracts.Users
{
    public class DeletionRequestDto
    {
        public Guid UserId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime DeletionRequestedOn { get; set; }
        public DateTime ScheduledDeletionTime { get; set; }
    }
}
