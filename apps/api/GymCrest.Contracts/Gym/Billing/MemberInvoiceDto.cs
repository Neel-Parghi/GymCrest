namespace GymCrest.Contracts.Gym.Billing
{
    public class MemberInvoiceDto
    {
        public string Id { get; set; } = string.Empty;
        public Guid MemberId { get; set; }
        public string MemberName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string BillingType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal Balance { get; set; }
        public DateTime DateIssued { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime CreatedOn { get; set; }
        public string Status { get; set; } = "Paid";
        public string PaymentMethod { get; set; } = string.Empty;
        public string MembershipNumber { get; set; } = string.Empty;
        public Guid RealRecordId { get; set; }

        // Dynamic Branch Information
        public Guid? BranchId { get; set; }
        public string BranchName { get; set; } = string.Empty;
        public string BranchLine1 { get; set; } = string.Empty;
        public string BranchLine2 { get; set; } = string.Empty;
        public string BranchCity { get; set; } = string.Empty;
        public string BranchState { get; set; } = string.Empty;
        public string BranchPostalCode { get; set; } = string.Empty;
    }
}
