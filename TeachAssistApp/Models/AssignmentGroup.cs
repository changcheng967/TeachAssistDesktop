using System.Collections.ObjectModel;
using System.Linq;
using TeachAssistApp.Helpers;

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

    public AssignmentImpact? Impact { get; set; }

    public bool HasAnyMark => Assignments.Any(a => a.MarkAchieved.HasValue && a.MarkPossible.HasValue);

    public string GradeColor
    {
        get
        {
            if (!HasAnyMark) return GradeColorHelper.NA;
            var validMarks = Assignments.Where(a => a.MarkAchieved.HasValue && a.MarkPossible.HasValue && a.MarkPossible.Value > 0).ToList();
            if (!validMarks.Any()) return GradeColorHelper.NA;
            var avgPercentage = validMarks.Average(a => a.Percentage ?? 0);
            return GradeColorHelper.GetColor(avgPercentage);
        }
    }
}
