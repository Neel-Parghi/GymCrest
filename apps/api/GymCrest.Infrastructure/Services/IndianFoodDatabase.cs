using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace GymCrest.Infrastructure.Services;


public class IndianFoodDatabase
{
    private const string ResourceName = "GymCrest.Data.indb-foods.json";

    private const double MinMatchScore = 0.75;

    private static readonly HashSet<string> Descriptors = new()
        { "washed", "plain", "simple", "boiled", "steamed", "cooked", "homemade", "home", "fresh", "whole", "basic" };

    private static readonly Regex NonLetters = new(@"[^a-z]+", RegexOptions.Compiled);

    private readonly List<(IndbFood Food, string[] Tokens)> _index;

    public IndianFoodDatabase()
    {
        using var stream = typeof(IndianFoodDatabase).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Embedded resource {ResourceName} not found.");
        var foods = JsonSerializer.Deserialize<List<IndbFood>>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();

        _index = foods
            .SelectMany(f => f.Aliases.Append(f.Name).Select(alias => (f, Tokenize(alias))))
            .Where(e => e.Item2.Length > 0)
            .ToList();
    }

    public int Count => _index.Select(e => e.Food.Code).Distinct().Count();

    public IndbFood? Match(IEnumerable<string> names)
    {
        IndbFood? best = null;
        double bestScore = 0;
        int bestLength = int.MaxValue;

        foreach (string[] query in names.Select(Tokenize).Where(t => t.Length > 0))
        {
            foreach (var (food, tokens) in _index)
            {
                double score = Score(query, tokens);
                if (score > bestScore || (score == bestScore && score > 0 && food.Name.Length < bestLength))
                {
                    best = food;
                    bestScore = score;
                    bestLength = food.Name.Length;
                }
            }
        }

        return bestScore >= MinMatchScore ? best : null;
    }

    private static double Score(string[] query, string[] candidate)
    {
        if (query.All(candidate.Contains))
        {
            int significant = candidate.Count(t => query.Contains(t) || !Descriptors.Contains(t));
            return (double)query.Length / significant;
        }

        if (candidate.All(query.Contains) && query.Length - candidate.Length == 1)
            return 0.9 * candidate.Length / query.Length;

        return 0;
    }

    private static string[] Tokenize(string text) =>
        NonLetters.Split(text.ToLowerInvariant())
            .Where(t => t.Length > 1)
            .Select(Singular)
            .Distinct()
            .ToArray();

    private static string Singular(string token) =>
        token.Length > 3 && token.EndsWith('s') && !token.EndsWith("ss") ? token[..^1] : token;
}

public sealed class IndbFood
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<string> Aliases { get; set; } = new();
    public double Kcal { get; set; }
    public double Protein { get; set; }
    public double Carbs { get; set; }
    public double Fats { get; set; }
    public string? Unit { get; set; }
    public double? UnitGrams { get; set; }
}
