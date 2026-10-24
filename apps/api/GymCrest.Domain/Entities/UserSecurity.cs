using System;

namespace GymCrest.Domain.Entities
{
    public class UserSecurity : BaseEntity
    {
        public Guid UserId { get; set; }
        
        public User User { get; set; } = null!;

        public bool IsEmailVerified { get; set; } = false;

        public string? OtpCode { get; set; }

        public DateTime? OtpExpiry { get; set; }

        public string? InvitationToken { get; set; }

        public DateTime? InvitationExpiry { get; set; }

        public bool IsInvitationAccepted { get; set; } = false;

        public DateTime? DeletionRequestedOn { get; set; }
    }
}
