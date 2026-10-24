namespace GymCrest.Contracts.SuperAdmin.Dashboard
{
    public class MetricDto
    {
        public decimal CurrentValue { get; set; }
        public decimal? PreviousValue { get; set; }
        public int? Progress { get; set; } 
    }
}
