using System.Collections.Generic;

namespace TeachAssistApp.Models;

public class Course
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Block { get; set; }
    public string Room { get; set; } = string.Empty;
    public string? StartTime { get; set; }
    public string? EndTime { get; set; }
    public object? OverallMark { get; set; } // Can be double or "N/A"
    public List<Assignment> Assignments { get; set; } = new();
    public WeightTable WeightTable { get; set; } = new();

    // For fetching detailed report from TeachAssist
    public string? SubjectId { get; set; }
    public string? StudentId { get; set; }
    public string? ReportUrl { get; set; } // Full URL from course list href
    public bool PartiallyParsed { get; set; } // True when parsed by fallback parser

    public string DisplayMark => OverallMark?.ToString() ?? "N/A";

    public bool HasValidMark => OverallMark is double mark && mark >= 0;

    public double? NumericMark => OverallMark as double?;

    public string GradeColor
    {
        get
        {
            if (!HasValidMark) return "#FF30363D"; // Subtle gray for N/A

            var mark = (double)OverallMark!;
            if (mark >= 95) return "#FF2EA043"; // Forest Green - A+
            if (mark >= 90) return "#FF3FB950"; // Green - A
            if (mark >= 85) return "#FF238636"; // Darker Green - A-
            if (mark >= 80) return "#FFD29922"; // Gold - B+
            if (mark >= 75) return "#FF9A6700"; // Darker Gold - B
            if (mark >= 70) return "#FFDB6D28"; // Orange - B-
            if (mark >= 65) return "#FFA57104"; // Dark Orange - C+
            if (mark >= 60) return "#FFf85149"; // Red - C
            return "#FFD73A49"; // Darker Red - Below C
        }
    }

    public string GradeLevel
    {
        get
        {
            if (!HasValidMark) return "N/A";

            var mark = (double)OverallMark!;
            if (mark >= 95) return "Level 4+ (Excellent!)";
            if (mark >= 90) return "Level 4 (Very Good!)";
            if (mark >= 85) return "Level 4- (Good)";
            if (mark >= 80) return "Level 3+ (Good)";
            if (mark >= 75) return "Level 3 (Satisfactory)";
            if (mark >= 70) return "Level 3- (Satisfactory)";
            if (mark >= 65) return "Level 2 (Passing)";
            if (mark >= 60) return "Level 2- (Struggling)";
            return "Level 1 (Below Expectations)";
        }
    }

    public string GradeLetter
    {
        get
        {
            if (!HasValidMark) return "N/A";

            var mark = (double)OverallMark!;
            if (mark >= 95) return "A+";
            if (mark >= 90) return "A";
            if (mark >= 85) return "A-";
            if (mark >= 80) return "B+";
            if (mark >= 75) return "B";
            if (mark >= 70) return "B-";
            if (mark >= 65) return "C+";
            if (mark >= 60) return "C";
            return "D";
        }
    }

    // New properties for CGC/Geography courses
    public bool IsCGCFormat { get; set; }
    public List<AssignmentTrend> AssignmentTrends { get; set; } = new();
}

public class AssignmentTrend
{
    public string AssignmentName { get; set; } = string.Empty;
    public double Mark { get; set; }
    public double Weight { get; set; }
    public string Expectation { get; set; } = string.Empty; // A1, A2, B1, etc.
    public string Type { get; set; } = "Product"; // Conversation, Observation, Product
}
