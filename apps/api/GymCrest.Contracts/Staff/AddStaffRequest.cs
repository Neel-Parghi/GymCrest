using GymCrest.Shared.Enums;

namespace GymCrest.Contracts.Staff
{
    public class AddStaffRequest
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public StaffRole Role { get; set; }
        public List<string>? Specializations { get; set; }
        public string? Bio { get; set; }
        public int? ExperienceYears { get; set; }
        public string? InstagramUrl { get; set; }
        public string? PortfolioUrl { get; set; }
        public string? ShiftTimings { get; set; }
        public Guid? BranchId { get; set; }
        public bool SendInvitation { get; set; } = true;
    }
}
