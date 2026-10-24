using System;
using System.Collections.Generic;

namespace GymCrest.Contracts.DietTracking
{
    public class DietLogDto
    {
        public Guid Id { get; set; }
        public Guid MemberId { get; set; }
        public DateTime LogDate { get; set; }

        public int TargetCalories { get; set; }
        public decimal TargetProtein { get; set; }
        public decimal TargetCarbs { get; set; }
        public decimal TargetFats { get; set; }

        public int TotalCalories { get; set; }
        public decimal TotalProtein { get; set; }
        public decimal TotalCarbs { get; set; }
        public decimal TotalFats { get; set; }

        public List<MealLogEntryDto> MealEntries { get; set; } = new();
        public List<AssignedMealDto> AssignedMeals { get; set; } = new();
    }

    public class MealLogEntryDto
    {
        public Guid Id { get; set; }
        public string MealType { get; set; } = string.Empty;
        public string FoodName { get; set; } = string.Empty;
        public int Calories { get; set; }
        public decimal Protein { get; set; }
        public decimal Carbs { get; set; }
        public decimal Fats { get; set; }
        public Guid? SourceDietPlanMealId { get; set; }
    }

    public class AssignedMealDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Time { get; set; } = string.Empty;
        public int Calories { get; set; }
        public decimal Protein { get; set; }
        public decimal Carbs { get; set; }
        public decimal Fats { get; set; }
        public string Items { get; set; } = string.Empty;
    }

    public class AddMealEntryRequestDto
    {
        public DateTime LogDate { get; set; }
        public string MealType { get; set; } = string.Empty;
        public string FoodName { get; set; } = string.Empty;
        public int Calories { get; set; }
        public decimal Protein { get; set; }
        public decimal Carbs { get; set; }
        public decimal Fats { get; set; }
        public Guid? SourceDietPlanMealId { get; set; }
    }

    public class UpdateMealEntryRequestDto
    {
        public string FoodName { get; set; } = string.Empty;
        public int Calories { get; set; }
        public decimal Protein { get; set; }
        public decimal Carbs { get; set; }
        public decimal Fats { get; set; }
    }

    public class DietLogSummaryDto
    {
        public DateTime LogDate { get; set; }
        public int TargetCalories { get; set; }
        public int TotalCalories { get; set; }
        public decimal TargetProtein { get; set; }
        public decimal TotalProtein { get; set; }
        public decimal TargetCarbs { get; set; }
        public decimal TotalCarbs { get; set; }
        public decimal TargetFats { get; set; }
        public decimal TotalFats { get; set; }
    }
}
