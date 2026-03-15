namespace TeachAssistApp.Models;

public class AssignmentImpact
{
    public string AssignmentName { get; set; } = string.Empty;
    public double ImpactDelta { get; set; }
    public bool IsPositive => ImpactDelta >= 0;
    public bool IsHighImpact { get; set; }
    public double CumulativeAfter { get; set; }
    public double CumulativeBefore { get; set; }

    public string DisplayImpact => $"{(ImpactDelta >= 0 ? "+" : "")}{ImpactDelta:F1}%";
    public string ImpactColor => IsPositive ? "#FF238636" : "#FFF85149";
}
