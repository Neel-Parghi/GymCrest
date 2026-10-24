namespace GymCrest.Contracts.SuperAdmin.Dashboard
{
    public class PlanDistributionDto
    {
        public string PlanName { get; set; } = string.Empty;
        public int Count { get; set; }
        public double Percentage { get; set; }
        public string Color { get; set; } = "#6366f1"; // Default Indigo
    }
}
