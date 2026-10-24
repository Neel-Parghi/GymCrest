using GymCrest.Shared.Enums;

namespace GymCrest.Domain.Entities
{
    public class User : BaseEntity
    {
        public string FirstName { get; set; } = string.Empty;
        
        public string LastName { get; set; } = string.Empty;
        
        public string Email { get; set; } = string.Empty;
        
        public string? PasswordHash { get; set; }

        public Guid? GymId { get; set; }

        public UserRole Role { get; set; }

        public bool IsActive { get; set; }

        public int AiPlanGenerationsCount { get; set; } = 0;

        public ICollection<RefreshToken> RefreshTokens { get; set; } = [];

        public UserSecurity? Security { get; set; }

        public UserProfile? Profile { get; set; }
        
        public UserPreference? Preference { get; set; }
        
        public ICollection<MemberMeasurement> Measurements { get; set; } = [];
        
        public ICollection<Gym>? Gyms { get; set; } = [];
    }
}
