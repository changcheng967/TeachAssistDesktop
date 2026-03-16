using System.IO;
using System.Text;
using TeachAssistApp.Models;
using System.Linq;

namespace TeachAssistApp.Services;

public class PdfExporter
{
    public async Task<string> GenerateGradeReportHtmlAsync(List<Course> courses, string studentName)
    {
        var html = new StringBuilder();

        var timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        html.AppendLine("<!DOCTYPE html>");
        html.AppendLine("<html>");
        html.AppendLine("<head>");
        html.AppendLine("<meta charset='utf-8'/>");
        html.AppendLine("<title>TeachAssist Grade Report</title>");
        html.AppendLine("<style>");
        html.AppendLine("body { font-family: 'Segoe UI', Arial, sans-serif; margin: 40px; background: #f5f5f5; }");
        html.AppendLine(".container { max-width: 900px; margin: 0 auto; background: white; padding: 40px; border-radius: 8px; box-shadow: 0 2px 8px rgba(0,0,0,0.1); }");
        html.AppendLine(".header { border-bottom: 2px solid #30363d; padding-bottom: 20px; margin-bottom: 30px; }");
        html.AppendLine(".header h1 { color: #0969da; margin: 0; font-size: 28px; }");
        html.AppendLine(".header p { color: #656d76; margin: 8px 0 0 0; }");
        html.AppendLine(".summary { display: flex; justify-content: space-between; margin: 30px 0; padding: 20px; background: #f6f8fa; border-radius: 6px; }");
        html.AppendLine(".summary-box { text-align: center; }");
        html.AppendLine(".summary-box .label { font-size: 12px; color: #656d76; text-transform: uppercase; letter-spacing: 0.5px; }");
        html.AppendLine(".summary-box .value { font-size: 32px; font-weight: bold; margin-top: 8px; }");
        html.AppendLine(".summary-box.excellent .value { color: #238636; }");
        html.AppendLine(".summary-box.good .value { color: #d29922; }");
        html.AppendLine(".summary-box.needs-work .value { color: #f85149; }");
        html.AppendLine(".course-list { margin-top: 30px; }");
        html.AppendLine(".course-item { border: 1px solid #d0d7de; border-radius: 6px; padding: 16px; margin-bottom: 16px; }");
        html.AppendLine(".course-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 12px; }");
        html.AppendLine(".course-code { font-size: 16px; font-weight: bold; color: #24292f; }");
        html.AppendLine(".course-mark { font-size: 24px; font-weight: bold; padding: 8px 16px; border-radius: 6px; color: white; }");
        html.AppendLine(".course-name { color: #656d76; font-size: 14px; margin-bottom: 8px; }");
        html.AppendLine(".course-details { font-size: 12px; color: #656d76; }");
        html.AppendLine(".footer { margin-top: 40px; padding-top: 20px; border-top: 1px solid #d0d7de; text-align: center; color: #656d76; font-size: 12px; }");
        html.AppendLine("</style>");
        html.AppendLine("</head>");
        html.AppendLine("<body>");
        html.AppendLine("<div class='container'>");

        // Header
        html.AppendLine("<div class='header'>");
        html.AppendLine("<h1>🎓 TeachAssist Grade Report</h1>");
        html.AppendLine($"<p>Generated: {timestamp}</p>");
        html.AppendLine("</div>");

        // Calculate stats
        var validCourses = courses.Where(c => c.HasValidMark).ToList();
        double overallAvg = 0;
        double highest = 0;
        double lowest = 100;

        if (validCourses.Any())
        {
            overallAvg = validCourses.Average(c => c.NumericMark ?? 0);
            highest = validCourses.Max(c => c.NumericMark ?? 0);
            lowest = validCourses.Min(c => c.NumericMark ?? 0);
        }

        // Summary section
        string summaryClass = overallAvg >= 80 ? "excellent" : overallAvg >= 70 ? "good" : "needs-work";

        html.AppendLine("<div class='summary'>");
        html.AppendLine($"<div class='summary-box {summaryClass}'>");
        html.AppendLine("<div class='label'>Overall Average</div>");
        html.AppendLine($"<div class='value'>{overallAvg:F1}%</div>");
        html.AppendLine("</div>");

        html.AppendLine("<div class='summary-box'>");
        html.AppendLine("<div class='label'>Highest Mark</div>");
        html.AppendLine($"<div class='value'>{highest:F1}%</div>");
        html.AppendLine("</div>");

        html.AppendLine("<div class='summary-box'>");
        html.AppendLine("<div class='label'>Lowest Mark</div>");
        html.AppendLine($"<div class='value'>{lowest:F1}%</div>");
        html.AppendLine("</div>");

        html.AppendLine("<div class='summary-box'>");
        html.AppendLine("<div class='label'>Total Courses</div>");
        html.AppendLine($"<div class='value'>{courses.Count}</div>");
        html.AppendLine("</div>");
        html.AppendLine("</div>");

        // Course list
        html.AppendLine("<div class='course-list'>");
        html.AppendLine("<h2 style='color: #24292f; margin-bottom: 20px;'>Course Details</h2>");

        foreach (var course in courses.OrderBy(c => c.Code))
        {
            var mark = course.DisplayMark;
            var color = course.GradeColor.Replace("#", "");

            html.AppendLine("<div class='course-item'>");
            html.AppendLine("<div class='course-header'>");
            html.AppendLine($"<span class='course-code'>{course.Code}</span>");

            if (course.HasValidMark)
            {
                html.AppendLine($"<span class='course-mark' style='background-color: #{color}'>{mark}</span>");
            }
            else
            {
                html.AppendLine($"<span class='course-mark' style='background-color: #30363d'>{mark}</span>");
            }

            html.AppendLine("</div>");
            html.AppendLine($"<div class='course-name'>{course.Name ?? "N/A"}</div>");
            html.AppendLine("<div class='course-details'>");

            if (!string.IsNullOrEmpty(course.Room))
            {
                html.AppendLine($"Room: {course.Room} | ");
            }

            html.AppendLine($"Block: {course.Block}");

            if (course.HasValidMark)
            {
                html.AppendLine($" | Level: {course.GradeLevel} ({course.GradeLetter})");
            }

            html.AppendLine("</div>");
            html.AppendLine("</div>");
        }

        html.AppendLine("</div>");

        // Footer
        html.AppendLine("<div class='footer'>");
        html.AppendLine("<p>Generated by TeachAssist Desktop App for YRDSB Students</p>");
        html.AppendLine("<p>This is an unofficial grade report. Please refer to the official TeachAssist website for authoritative information.</p>");
        html.AppendLine("</div>");

        html.AppendLine("</div>");
        html.AppendLine("</body>");
        html.AppendLine("</html>");

        return html.ToString();
    }

    public async Task<string> SaveAndOpenPdfAsync(string html, string outputPath)
    {
        // Save HTML file
        await File.WriteAllTextAsync(outputPath, html);

        // Open with default browser (user can print to PDF from browser)
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = outputPath,
            UseShellExecute = true
        });

        return outputPath;
    }
}
