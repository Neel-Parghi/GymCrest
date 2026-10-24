namespace GymCrest.Contracts.Gym.Management
{
    public class UpdateGymDto
    {
        public Guid Id { get; set; }
        public string GymName { get; set; } = string.Empty;
        public string BrandName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? WebsiteUrl { get; set; }
        public string? Description { get; set; }
        public string? GstNumber { get; set; }
        public string? RegistrationNumber { get; set; }
        public bool IsActive { get; set; }
        public bool IsVerified { get; set; }
    }
}
