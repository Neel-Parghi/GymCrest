namespace GymCrest.Contracts.Users
{
    public class StandaloneUserDto
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName => $"{FirstName} {LastName}";
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string ProfilePictureUrl { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime CreatedOn { get; set; }
        public DateTime? DeletionRequestedOn { get; set; }
        public bool IsEmailVerified { get; set; }
    }
}
