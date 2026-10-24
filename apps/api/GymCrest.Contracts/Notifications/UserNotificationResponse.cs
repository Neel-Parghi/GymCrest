namespace GymCrest.Contracts.Notifications
{
    public class UserNotificationResponse
    {
        public Guid Id { get; set; }
        public Guid GymId { get; set; }
        public Guid? BranchId { get; set; }
        public Guid UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public DateTime CreatedOn { get; set; }
    }
}
