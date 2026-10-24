using GymCrest.Shared.Enums;

namespace GymCrest.Contracts.Staff
{
    public class StaffResponse
    {
        public Guid Id { get; set; }
        public string StaffNumber { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public StaffRole Role { get; set; }
        public string? ProfilePictureUrl { get; set; }
        
        // Trainer Specifics
        public List<string>? Specializations { get; set; }
        public string? Bio { get; set; }
        public int? ExperienceYears { get; set; }
        
        // Socials
        public string? InstagramUrl { get; set; }
        public string? PortfolioUrl { get; set; }
        
        // Operations
        public string? ShiftTimings { get; set; }
        public bool IsActive { get; set; }
        public DateTime JoiningDate { get; set; }
        public Guid? BranchId { get; set; }
        public bool IsCheckedIn { get; set; }
        public DateTime? LastCheckInTime { get; set; }
    }
}
