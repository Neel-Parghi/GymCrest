using System;
using System.ComponentModel.DataAnnotations.Schema;
using GymCrest.Domain.Interface;

namespace GymCrest.Domain.Entities
{
    public class StaffPayoutLog : BaseEntity, IBranchScoped
    {
        public Guid GymId { get; set; }
        public Guid? BranchId { get; set; }

        public Guid StaffId { get; set; }

        [ForeignKey("StaffId")]
        public virtual Staff Staff { get; set; } = null!;

        public string MonthKey { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal BaseSalarySnapshot { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Commissions { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPayout { get; set; } = 0;

        public string Status { get; set; } = "Pending";

        public DateTime? PayoutDate { get; set; }
    }
}
