using GymCrest.Shared.Enums;

namespace GymCrest.Domain.Entities
{
    public class UserProfile : BaseEntity
    {
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        public string Phone { get; set; } = string.Empty;
        public string ProfilePictureUrl { get; set; } = string.Empty;
        
        public DateTime? DateOfBirth { get; set; }
        public Gender? Gender { get; set; }

        public Guid? AddressId { get; set; }
        public Address? Address { get; set; }

        public bool IsOnboarded { get; set; } = false;

        public int CurrentOnboardingStep { get; set; } = 0;
    }
}
