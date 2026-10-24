using System.ComponentModel.DataAnnotations;

namespace GymCrest.Domain.Entities
{
    public class RefreshToken : BaseEntity
    {
        [Required]
        public string Token { get; set; } = string.Empty;

        public DateTime Expires { get; set; }

        public bool IsExpired => DateTime.UtcNow >= Expires;

        public DateTime? Revoked { get; set; }

        public DateTime? GracePeriodExpires { get; set; }

        public string? ReplacedByToken { get; set; }

        public bool IsActive => !IsExpired && (Revoked == null || (GracePeriodExpires != null && DateTime.UtcNow < GracePeriodExpires));

        public Guid UserId { get; set; }

        public User? User { get; set; }
    }
}
