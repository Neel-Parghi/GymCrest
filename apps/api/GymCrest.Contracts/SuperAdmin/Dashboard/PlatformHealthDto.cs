namespace GymCrest.Contracts.SuperAdmin.Dashboard
{
    public class PlatformHealthDto
    {
        public int PendingVerifications { get; set; }
        public string? Status { get; set; } 
        public string? LastCheck { get; set; }
    }
}
