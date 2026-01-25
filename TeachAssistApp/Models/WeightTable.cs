using System.Collections.Generic;

namespace TeachAssistApp.Models;

public class WeightTable
{
    public Dictionary<string, double> Weights { get; set; } = new();

    public double? GetWeight(string category)
    {
        return Weights.TryGetValue(category, out var weight) ? weight : null;
    }

    public void SetWeight(string category, double weight)
    {
        Weights[category] = weight;
    }
}
