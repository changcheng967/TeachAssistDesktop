namespace TeachAssistApp.Models;

public class Assignment
{
    public string Name { get; set; } = string.Empty;
    public string? Date { get; set; }
    public double? MarkAchieved { get; set; }
    public double? MarkPossible { get; set; }
    public string Category { get; set; } = string.Empty;
    public double? Weight { get; set; }
    public string? Feedback { get; set; }

    public double? Percentage
    {
        get
        {
            if (MarkAchieved.HasValue && MarkPossible.HasValue && MarkPossible.Value > 0)
            {
                return (MarkAchieved.Value / MarkPossible.Value) * 100;
            }
            return null;
        }
    }

    public bool IsMissing => !MarkAchieved.HasValue && MarkPossible.HasValue;
}
