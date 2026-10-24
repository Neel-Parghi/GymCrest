namespace GymCrest.Contracts.Gym.Billing
{
    public class MemberBillingOverviewDto
    {
        public MemberBillingStatsDto Stats { get; set; } = new();
        public List<MemberInvoiceDto> Invoices { get; set; } = new();
        public string GymName { get; set; } = string.Empty;
        public string GymBrandName { get; set; } = string.Empty;
        public string GymGstNumber { get; set; } = string.Empty;
    }

    public class MemberBillingStatsDto
    {
        public decimal TotalCollected { get; set; }
        public decimal PendingReceivables { get; set; }
        public decimal OverdueBalances { get; set; }
        public decimal TotalInvoiced { get; set; }
    }
}
