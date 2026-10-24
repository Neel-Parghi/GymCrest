using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GymCrest.Application.Modules.Diet.Interfaces;
using GymCrest.Contracts.DietTracking;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace GymCrest.Infrastructure.Services;

public class NutritionApiService : INutritionApiService
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromDays(7);
    private static readonly Regex Whitespace = new(@"\s+", RegexOptions.Compiled);

    private const double MaxCalorieDisagreement = 1.5;
    private const double MaxMacroDisagreement = 2.0;

    private const double CrossCheckMinKcal = 15;
    private const double MacroToleranceGrams = 8;

    private static readonly TimeSpan UsdaItemTimeout = TimeSpan.FromSeconds(4);

    private static readonly HashSet<string> MassUnits = new(StringComparer.OrdinalIgnoreCase)
        { "g", "gm", "gms", "gram", "grams", "kg", "ml", "l", "litre", "liter", "oz" };

    private static readonly HashSet<string> ContainerUnits = new(StringComparer.OrdinalIgnoreCase)
        { "bowl", "small bowl", "soup bowl", "plate", "cup", "tea cup", "glass", "tall glass", "tall stemmed glass",
          "jar", "dish", "katori", "serving", "tablespoon", "teaspoon", "tbsp", "tsp", "spoon", "ladle", "handful" };

    private readonly GeminiFoodSearchProvider _geminiProvider;
    private readonly IndianFoodDatabase _indianFoods;
    private readonly UsdaFoodDataProvider _usdaProvider;
    private readonly IMemoryCache _cache;
    private readonly ILogger<NutritionApiService> _logger;

    public NutritionApiService(
        GeminiFoodSearchProvider geminiProvider,
        IndianFoodDatabase indianFoods,
        UsdaFoodDataProvider usdaProvider,
        IMemoryCache cache,
        ILogger<NutritionApiService> logger)
    {
        _geminiProvider = geminiProvider;
        _indianFoods = indianFoods;
        _usdaProvider = usdaProvider;
        _cache = cache;
        _logger = logger;
    }

    public async Task<FoodNutritionDto?> GetNutritionForFoodAsync(string query)
    {
        string cacheKey = $"food-search:{Whitespace.Replace(query.Trim().ToLowerInvariant(), " ")}";
        if (_cache.TryGetValue(cacheKey, out FoodNutritionDto? cached))
        {
            return cached;
        }

        FoodNutritionDto? result = await SearchAsync(query);
        if (result != null)
        {
            _cache.Set(cacheKey, result, CacheDuration);
        }
        return result;
    }

    private async Task<FoodNutritionDto?> SearchAsync(string query)
    {
        GeminiMeal? meal = await _geminiProvider.ParseMealAsync(query);
        if (meal == null)
        {
            _logger.LogInformation("Meal parsing unavailable for '{Query}', falling back to direct database search.", query);
            return await SearchWithoutParsingAsync(query);
        }

        string aiSource = meal.Grounded ? "web" : "ai";
        FoodNutritionItemDto[] items = await Task.WhenAll(meal.Items.Select(i => ResolveItemAsync(i, aiSource)));

        bool anyEstimate = items.Any(i => i.Source == aiSource);
        bool allEstimate = items.All(i => i.Source == aiSource);

        return new FoodNutritionDto
        {
            Name = string.Join(", ", items.Select(i => i.Name)),
            Calories = Math.Round(items.Sum(i => i.Calories)),
            Protein = Math.Round(items.Sum(i => i.Protein), 1),
            Carbs = Math.Round(items.Sum(i => i.Carbs), 1),
            Fats = Math.Round(items.Sum(i => i.Fats), 1),
            Items = items.ToList(),
            Source = allEstimate ? aiSource : anyEstimate ? "mixed" : "database",
            IsEstimate = anyEstimate
        };
    }

    private async Task<FoodNutritionItemDto> ResolveItemAsync(GeminiFoodItem item, string aiSource)
    {
        if (!item.Branded)
        {
            var fromIndb = FromIndianDatabase(item);
            if (fromIndb != null) return fromIndb;

            var fromUsda = await FromUsdaAsync(item);
            if (fromUsda != null) return fromUsda;
        }

        return new FoodNutritionItemDto
        {
            Name = item.Name,
            Quantity = item.Quantity ?? string.Empty,
            Calories = item.Calories,
            Protein = item.Protein,
            Carbs = item.Carbs,
            Fats = item.Fats,
            Source = aiSource
        };
    }

    private FoodNutritionItemDto? FromIndianDatabase(GeminiFoodItem item)
    {
        IndbFood? food = _indianFoods.Match((item.Aliases ?? new()).Prepend(item.Name));
        if (food == null) return null;
        string unit = item.Unit ?? string.Empty;
        bool countedPieces = item.Count > 0
            && food.Unit != null && food.UnitGrams is > 0
            && !ContainerUnits.Contains(food.Unit)
            && !MassUnits.Contains(unit) && !ContainerUnits.Contains(unit);

        double grams = countedPieces ? item.Count * food.UnitGrams!.Value : item.Grams;
        if (grams <= 0) return null;

        var result = Scale(item, food.Name, food.Kcal, food.Protein, food.Carbs, food.Fats, grams, "indb");
        if (countedPieces)
        {
            result.Quantity = $"{FormatCount(item.Count)} {food.Unit}";
        }

        return AgreesWithEstimate(result, item) ? result : null;
    }

    private async Task<FoodNutritionItemDto?> FromUsdaAsync(GeminiFoodItem item)
    {
        if (item.Grams <= 0) return null;

        (string Name, double Calories, double Protein, double Carbs, double Fats)? match;
        try
        {
            match = await _usdaProvider.FindPer100gAsync(item.Name).WaitAsync(UsdaItemTimeout);
        }
        catch (TimeoutException)
        {
            _logger.LogInformation("USDA lookup for '{Food}' timed out; using the AI estimate.", item.Name);
            return null;
        }
        if (match == null) return null;

        var (name, kcal, protein, carbs, fats) = match.Value;
        var result = Scale(item, name, kcal, protein, carbs, fats, item.Grams, "usda");
        return AgreesWithEstimate(result, item) ? result : null;
    }

    private bool AgreesWithEstimate(FoodNutritionItemDto fromDatabase, GeminiFoodItem estimate)
    {
        bool agrees = (estimate.Calories < CrossCheckMinKcal || Within(fromDatabase.Calories, estimate.Calories, MaxCalorieDisagreement))
            && MacroAgrees(fromDatabase.Protein, estimate.Protein)
            && MacroAgrees(fromDatabase.Carbs, estimate.Carbs)
            && MacroAgrees(fromDatabase.Fats, estimate.Fats);

        if (agrees) return true;

        _logger.LogInformation(
            "Rejected {Source} match '{Reference}' for '{Food}': {DbKcal} kcal P{DbP} C{DbC} F{DbF} vs AI estimate {AiKcal} kcal P{AiP} C{AiC} F{AiF}.",
            fromDatabase.Source, fromDatabase.Reference, estimate.Name,
            fromDatabase.Calories, fromDatabase.Protein, fromDatabase.Carbs, fromDatabase.Fats,
            estimate.Calories, estimate.Protein, estimate.Carbs, estimate.Fats);
        return false;
    }

    private static bool MacroAgrees(double value, double estimate) =>
        Math.Abs(value - estimate) <= MacroToleranceGrams || Within(value, estimate, MaxMacroDisagreement);

    private static bool Within(double value, double reference, double factor) =>
        value >= reference / factor && value <= reference * factor;

    private static FoodNutritionItemDto Scale(GeminiFoodItem item, string reference,
        double kcalPer100, double proteinPer100, double carbsPer100, double fatsPer100, double grams, string source)
    {
        double factor = grams / 100.0;
        return new FoodNutritionItemDto
        {
            Name = item.Name,
            Quantity = item.Quantity ?? string.Empty,
            Calories = Math.Round(kcalPer100 * factor),
            Protein = Math.Round(proteinPer100 * factor, 1),
            Carbs = Math.Round(carbsPer100 * factor, 1),
            Fats = Math.Round(fatsPer100 * factor, 1),
            Source = source,
            Reference = reference
        };
    }

    private static string FormatCount(double count) =>
        count % 1 == 0 ? ((int)count).ToString() : count.ToString("0.#");

    private async Task<FoodNutritionDto?> SearchWithoutParsingAsync(string query)
    {
        var result = await _usdaProvider.SearchAsync(query);
        if (result != null)
        {
            result.Source = "usda";
        }
        return result;
    }
}
