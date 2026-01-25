using System.Collections.ObjectModel;
using System.Linq;

namespace TeachAssistApp.Models;

public class AssignmentGroup
{
    public string Name { get; set; } = string.Empty;
    public ObservableCollection<Assignment> Assignments { get; set; } = new();

    public Assignment? KuMark => Assignments.FirstOrDefault(a => a.Category == "KU");
    public Assignment? TMark => Assignments.FirstOrDefault(a => a.Category == "T");
    public Assignment? CMark => Assignments.FirstOrDefault(a => a.Category == "C");
    public Assignment? AMark => Assignments.FirstOrDefault(a => a.Category == "A");
    public Assignment? FMark => Assignments.FirstOrDefault(a => a.Category == "F");
    public Assignment? OMark => Assignments.FirstOrDefault(a => a.Category == "O");

    public bool HasAnyMark => Assignments.Any(a => a.MarkAchieved.HasValue && a.MarkPossible.HasValue);

    public string GradeColor
    {
        get
        {
            if (!HasAnyMark) return "#FF30363D";

            // Calculate average percentage across all categories
            var validMarks = Assignments.Where(a => a.MarkAchieved.HasValue && a.MarkPossible.HasValue && a.MarkPossible.Value > 0).ToList();
            if (!validMarks.Any()) return "#FF30363D";

            var avgPercentage = validMarks.Average(a => a.Percentage ?? 0);

            if (avgPercentage >= 95) return "#FF2EA043";
            if (avgPercentage >= 90) return "#FF3FB950";
            if (avgPercentage >= 85) return "#FF238636";
            if (avgPercentage >= 80) return "#FFD29922";
            if (avgPercentage >= 75) return "#FF9A6700";
            if (avgPercentage >= 70) return "#FFDB6D28";
            if (avgPercentage >= 65) return "#FFA57104";
            if (avgPercentage >= 60) return "#FFf85149";
            return "#FFD73A49";
        }
    }
}
