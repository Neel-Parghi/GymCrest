using System.ComponentModel.DataAnnotations.Schema;
using GymCrest.Domain.Interface;

namespace GymCrest.Domain.Entities
{
    public class CustomInvoice : BaseEntity, IBranchScoped
    {
        public Guid MemberId { get; set; }
        
        [ForeignKey("MemberId")]
        public virtual GymMember Member { get; set; } = null!;

        public Guid? TrainerId { get; set; }

        [ForeignKey("TrainerId")]
        public virtual Staff? Trainer { get; set; }

        public string BillingType { get; set; } = string.Empty; 
        public string Description { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal AmountPaid { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TaxRate { get; set; } = 18;

        public string PaymentMethod { get; set; } = "UPI";
        public string Status { get; set; } = "Paid"; 
        
        public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
        public DateTime DueDate { get; set; } = DateTime.UtcNow.AddDays(15);

        public Guid GymId { get; set; }

        [ForeignKey("GymId")]
        public virtual Gym Gym { get; set; } = null!;

        public Guid? BranchId { get; set; }

        [ForeignKey("BranchId")]
        public virtual Branch? Branch { get; set; }
    }
}
