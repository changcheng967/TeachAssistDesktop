namespace TeachAssistApp.Models;

public class GradeTimelinePoint
{
    public int Index { get; set; }
    public string AssignmentName { get; set; } = string.Empty;
    public string? Date { get; set; }
    public double CumulativeGrade { get; set; }
    public double Impact { get; set; }
    public bool IsHighImpact { get; set; }
    public bool FirstPoint { get; set; }
}
