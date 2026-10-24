namespace GymCrest.Contracts.DietTracking;

public class FoodNutritionDto
{
    public string Name { get; set; } = string.Empty;
    public double Calories { get; set; }
    public double Protein { get; set; }
    public double Carbs { get; set; }
    public double Fats { get; set; }

    public List<FoodNutritionItemDto> Items { get; set; } = new();
    public string Source { get; set; } = string.Empty;
    public bool IsEstimate { get; set; }
}

public class FoodNutritionItemDto
{
    public string Name { get; set; } = string.Empty;
    public string Quantity { get; set; } = string.Empty;
    public double Calories { get; set; }
    public double Protein { get; set; }
    public double Carbs { get; set; }
    public double Fats { get; set; }

    public string Source { get; set; } = string.Empty;

    public string? Reference { get; set; }
}
