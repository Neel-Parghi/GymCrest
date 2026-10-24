namespace GymCrest.Contracts.SaaSPayments
{
    public class PaymentStatsDto
    {
        public decimal TotalRevenue { get; set; }
        public decimal MonthlyRecurringRevenue { get; set; }
        public int ActiveSubscriptions { get; set; }
    }
}
