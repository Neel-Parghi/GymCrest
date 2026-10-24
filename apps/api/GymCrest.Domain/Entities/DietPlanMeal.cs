namespace GymCrest.Domain.Entities
{
    public class DietPlanMeal : BaseEntity
    {
        public Guid DietPlanId { get; set; }
        public DietPlan? DietPlan { get; set; }

        public string Name { get; set; } = string.Empty;
        public string Time { get; set; } = "08:00 AM";
        public int Calories { get; set; }
        public decimal Protein { get; set; }
        public decimal Carbs { get; set; }
        public decimal Fats { get; set; }
        public string Items { get; set; } = string.Empty;
        public int SortOrder { get; set; }
    }
}
