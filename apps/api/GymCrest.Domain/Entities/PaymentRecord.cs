using System.ComponentModel.DataAnnotations.Schema;
using GymCrest.Domain.Interface;
using GymCrest.Shared.Enums;

namespace GymCrest.Domain.Entities
{
    public class PaymentRecord : BaseEntity, IBranchScoped
    {
        public Guid MemberId { get; set; }

        [ForeignKey("MemberId")]
        public virtual GymMember Member { get; set; } = null!;

        public PaymentSourceType SourceType { get; set; }
        public Guid SourceId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public string PaymentMethod { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public DateTime PaidAt { get; set; } = DateTime.UtcNow;

        public Guid GymId { get; set; }

        [ForeignKey("GymId")]
        public virtual Gym Gym { get; set; } = null!;

        public Guid? BranchId { get; set; }

        [ForeignKey("BranchId")]
        public virtual Branch? Branch { get; set; }
    }
}
