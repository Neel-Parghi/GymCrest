using GymCrest.Domain.Interface;

namespace GymCrest.Domain.Entities
{
    public class StaffAttendanceLog : BaseEntity, IBranchScoped
    {
        public Guid GymId { get; set; }
        public Gym Gym { get; set; } = null!;

        public Guid? BranchId { get; set; }
        public Branch? Branch { get; set; }

        public Guid StaffId { get; set; }
        public Staff Staff { get; set; } = null!;

        public DateTime CheckInTime { get; set; }
        public DateTime? CheckOutTime { get; set; }
        public string? Notes { get; set; }
    }
}
