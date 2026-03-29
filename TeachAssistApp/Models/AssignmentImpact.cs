namespace TeachAssistApp.Models;

public class AssignmentImpact
{
    public string AssignmentName { get; set; } = string.Empty;

    /// <summary>
    /// How much the running weighted grade changed when this assignment was added.
    /// Positive = grade went up, negative = grade went down.
    /// </summary>
    public double ImpactDelta { get; set; }

    /// <summary>
    /// How much this assignment contributes to the final weighted grade.
    /// E.g., a KU test where KU=25%, 3 KU assignments, scored 90% → 90 * 25 / (3 * 75) = 10.0%
    /// </summary>
    public double WeightedContribution { get; set; }

    public bool IsPositive => ImpactDelta >= 0;
    public bool IsHighImpact { get; set; }
    public double CumulativeAfter { get; set; }
    public double CumulativeBefore { get; set; }

    public string DisplayImpact => $"{(ImpactDelta >= 0 ? "+" : "")}{ImpactDelta:F1}%";
    public string DisplayContribution => $"{WeightedContribution:F1}%";
    public string ImpactColor => IsPositive ? "#FF238636" : "#FFF85149";
}
