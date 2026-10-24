using System.Threading.Tasks;
using GymCrest.Contracts.DietTracking;

namespace GymCrest.Application.Modules.Diet.Interfaces;

public interface INutritionApiService
{
    Task<FoodNutritionDto?> GetNutritionForFoodAsync(string query);
}
