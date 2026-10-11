namespace GymForge.Contracts.DietPlan
{
    public class DietPlanDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Protein { get; set; }
        public decimal Carbs { get; set; }
        public decimal Fats { get; set; }
        public int Calories { get; set; }
        public string Goal { get; set; } = "Maintenance";
        public string? DietType { get; set; } // veg, eggetarian, non_veg, vegan;
        public Guid? GymId { get; set; }
        public Guid CreatedBy { get; set; }
        public bool IsCustom { get; set; } = false;
        public bool IsTemplate { get; set; } = true;
        public List<DietPlanMealDto> Meals { get; set; } = [];
    }

    public class DietPlanMealDto
    {
        public Guid Id { get; set; }
        public Guid DietPlanId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Time { get; set; } = "08:00 AM";
        public int Calories { get; set; }
        public decimal Protein { get; set; }
        public decimal Carbs { get; set; }
        public decimal Fats { get; set; }
        public string Items { get; set; } = string.Empty;
        public int SortOrder { get; set; }
    }

    public class CreateDietPlanRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Protein { get; set; }
        public decimal Carbs { get; set; }
        public decimal Fats { get; set; }
        public string Goal { get; set; } = "Maintenance";
        public string? DietType { get; set; } // veg, eggetarian, non_veg, vegan;
        public bool IsCustom { get; set; } = false;
        public bool IsTemplate { get; set; } = true;
        public List<CreateDietPlanMealDto> Meals { get; set; } = [];
    }

    public class UpdateDietPlanRequest
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Protein { get; set; }
        public decimal Carbs { get; set; }
        public decimal Fats { get; set; }
        public string Goal { get; set; } = "Maintenance";
        public string? DietType { get; set; } // veg, eggetarian, non_veg, vegan;
        public bool IsCustom { get; set; } = false;
        public bool IsTemplate { get; set; } = true;
        public List<CreateDietPlanMealDto> Meals { get; set; } = [];
    }

    public class CreateDietPlanMealDto
    {
        public string Name { get; set; } = string.Empty;
        public string Time { get; set; } = "08:00 AM";
        public int Calories { get; set; }
        public decimal Protein { get; set; }
        public decimal Carbs { get; set; }
        public decimal Fats { get; set; }
        public string Items { get; set; } = string.Empty;
        public int SortOrder { get; set; }
    }

    public class AssignDietPlanRequest
    {
        public Guid DietPlanId { get; set; }
    }

    public class MemberDietAssignmentDto
    {
        public Guid Id { get; set; }
        public Guid? MemberId { get; set; }
        public Guid? UserId { get; set; }
        public Guid DietPlanId { get; set; }
        public DateTime AssignedAt { get; set; }
        public bool IsActive { get; set; }
        public DietPlanDto? DietPlan { get; set; }
    }
}
