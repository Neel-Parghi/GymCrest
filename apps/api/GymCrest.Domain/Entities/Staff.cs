using GymCrest.Shared.Enums;
using GymCrest.Domain.Interface;

namespace GymCrest.Domain.Entities
{
    public class Staff : BaseEntity, IBranchScoped
    {
        public Guid GymId { get; set; }
        public Guid? BranchId { get; set; }
        public string StaffNumber { get; set; } = string.Empty;
        
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        
        public Guid? UserId { get; set; }
        public User? User { get; set; }
        
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
        public bool IsActive { get; set; } = true;
        public DateTime JoiningDate { get; set; }
        public bool IsCheckedIn { get; set; } = false;
        public DateTime? LastCheckInTime { get; set; }

        // Navigation
        public Gym Gym { get; set; } = null!;
        public Branch? Branch { get; set; }
        public ICollection<PTAssignment> PTAssignments { get; set; } = new List<PTAssignment>();
    }
}
