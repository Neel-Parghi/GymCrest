namespace GymCrest.Contracts.SuperAdmin.Dashboard
{
    public class RecentGymRegistrationDto
    {
        public Guid Id { get; set; }
        public string GymName { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
        public DateTime DateJoined { get; set; }
        public string Tier { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Initials { get; set; } = string.Empty;
    }
}
