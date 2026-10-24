using System.ComponentModel.DataAnnotations.Schema;

namespace GymCrest.Domain.Entities
{
    public class GymConfiguration : BaseEntity
    {
        public Guid GymId { get; set; }

        public int PlanExpirationTriggerDays { get; set; } = 7;

        public string? RazorpayKeyId { get; set; }
        public string? RazorpayKeySecret { get; set; }

        [ForeignKey(nameof(GymId))]
        public Gym Gym { get; set; } = null!;
    }
}
