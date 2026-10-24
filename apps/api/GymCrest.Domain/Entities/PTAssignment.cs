namespace GymCrest.Domain.Entities
{
    public class PTAssignment : BaseEntity
    {
        public Guid TrainerId { get; set; }
        public Guid MemberId { get; set; }
        
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        
        public string? SessionFrequency { get; set; }
        public string? PreferredSlot { get; set; }
        
        public bool IsActive { get; set; } = true;

        // Navigation
        public Staff Trainer { get; set; } = null!;
        public GymMember Member { get; set; } = null!;
    }
}
