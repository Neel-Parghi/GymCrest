using GymCrest.Domain.Interface;

namespace GymCrest.Domain.Entities
{
    public class AttendanceLog : BaseEntity, IBranchScoped
    {
        public Guid? BranchId { get; set; }
        public Branch? Branch { get; set; }

        public Guid MemberId { get; set; }
        public GymMember Member { get; set; } = null!;

        public DateTime CheckInTime { get; set; }
        public DateTime? CheckOutTime { get; set; }

        public Guid? VerifiedByUserId { get; set; }
        public string VerificationMethod { get; set; } = "Manual";
    }
}
