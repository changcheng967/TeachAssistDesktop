using System.IO;
using System.Text;
using TeachAssistApp.Models;
using TeachAssistApp.Helpers;
using System.Linq;

namespace TeachAssistApp.Services;

/// <summary>
/// Generates a printable, grade-centric HTML report and opens it in the default
/// browser so the student can print it (Ctrl+P) to PDF. Colors mirror the in-app
/// <see cref="GradeColorHelper"/> palette so the report matches what the student sees.
/// </summary>
public class PdfExporter
{
    public async Task<string> GenerateGradeReportHtmlAsync(List<Course> courses, string studentName)
    {
        var valid = courses.Where(c => c.HasValidMark).ToList();
        double overallAvg = valid.Any() ? valid.Average(c => c.NumericMark ?? 0) : 0;
        double highest = valid.Any() ? valid.Max(c => c.NumericMark ?? 0) : 0;
        double lowest = valid.Any() ? valid.Min(c => c.NumericMark ?? 0) : 0;
        double gpa = valid.Any() ? valid.Average(c => GpaPoints(c.NumericMark ?? 0)) : 0;
        string avgCss = CssColor(valid.Any() ? overallAvg : null);
        string student = string.IsNullOrWhiteSpace(studentName) ? "Student" : studentName;
        var timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm");

        var sb = new StringBuilder();
        sb.Append("<!DOCTYPE html><html lang='en'><head><meta charset='utf-8'/>");
        sb.Append("<meta name='viewport' content='width=device-width, initial-scale=1'/>");
        sb.Append("<title>TeachAssist Grade Report</title><style>");
        sb.Append(Css);
        sb.Append("</style></head><body><div class='page'>");

        // ── Header ───────────────────────────────────────────────
        sb.Append("<header class='hero' style='--accent:").Append(avgCss).Append(";'>");
        sb.Append("<div class='hero-top'>");
        sb.Append("<div class='brand'><span class='logo'>🎓</span><div><div class='brand-title'>TeachAssist Desktop</div>");
        sb.Append("<div class='brand-sub'>Grade Report</div></div></div>");
        sb.Append("<div class='hero-meta'>");
        sb.Append("<div class='student'>").Append(Escape(student)).Append("</div>");
        sb.Append("<div class='timestamp'>Generated ").Append(timestamp).Append("</div>");
        sb.Append("</div>");
        sb.Append("</div>");

        // Hero overall average
        sb.Append("<div class='hero-avg'>");
        sb.Append("<div class='avg-label'>Overall Average</div>");
        sb.Append("<div class='avg-value' style='color:").Append(avgCss).Append(";'>").Append($"{overallAvg:F1}<span class='pct'>%</span></div>");
        sb.Append("<div class='avg-note'>").Append(SchoolLabel(overallAvg)).Append("</div>");
        sb.Append("</div>");
        sb.Append("</header>");

        // ── Summary tiles ────────────────────────────────────────
        sb.Append("<section class='summary'>");
        AppendTile(sb, "Highest", $"{highest:F1}%", CssColor(highest));
        AppendTile(sb, "Lowest", $"{lowest:F1}%", CssColor(lowest));
        AppendTile(sb, "GPA (4.0)", $"{gpa:F2}", "#6366F1");
        AppendTile(sb, "Courses", $"{courses.Count}", "#6366F1");
        sb.Append("</section>");

        // ── Course list ──────────────────────────────────────────
        sb.Append("<section class='courses'><h2>Course Details</h2>");
        foreach (var course in courses.OrderBy(c => c.Code))
        {
            var mark = course.NumericMark;
            var color = CssColor(course.HasValidMark ? mark : null);
            sb.Append("<article class='course'>");
            sb.Append("<span class='badge' style='background:").Append(color).Append(";'>").Append(Escape(course.DisplayMark)).Append("</span>");
            sb.Append("<div class='course-main'>");
            sb.Append("<div class='course-code'>").Append(Escape(course.Code)).Append("</div>");
            sb.Append("<div class='course-name'>").Append(Escape(course.Name ?? "N/A")).Append("</div>");
            sb.Append("<div class='course-meta'>");
            if (!string.IsNullOrEmpty(course.Room)) sb.Append("<span>Room ").Append(Escape(course.Room)).Append("</span>");
            sb.Append("<span>Block ").Append(course.Block).Append("</span>");
            if (course.HasValidMark) sb.Append("<span>").Append(course.GradeLevel).Append(" · ").Append(course.GradeLetter).Append("</span>");
            if (!string.IsNullOrEmpty(course.MarkStatus)) sb.Append("<span class='status'>").Append(Escape(course.MarkStatus)).Append("</span>");
            sb.Append("</div></div></article>");
        }
        sb.Append("</section>");

        // ── Legend + footer ──────────────────────────────────────
        sb.Append("<section class='legend no-print'><span class='legend-title'>Grade scale</span>");
        AppendLegend(sb, "90+", GradeColorHelper.Tier90);
        AppendLegend(sb, "80–89", GradeColorHelper.Tier80);
        AppendLegend(sb, "70–79", GradeColorHelper.Tier70);
        AppendLegend(sb, "60–69", GradeColorHelper.Tier60);
        AppendLegend(sb, "<60", GradeColorHelper.Below60);
        sb.Append("</section>");

        sb.Append("<footer>Generated by TeachAssist Desktop · Unofficial report — refer to the official TeachAssist portal for authoritative marks.</footer>");
        sb.Append("</div></body></html>");

        return await Task.FromResult(sb.ToString());
    }

    public async Task<string> SaveAndOpenPdfAsync(string html, string outputPath)
    {
        await File.WriteAllTextAsync(outputPath, html);

        // Open with the default browser; the user prints to PDF from there (Ctrl+P).
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = outputPath,
            UseShellExecute = true
        });

        return outputPath;
    }

    // ── helpers ──────────────────────────────────────────────────
    private static string CssColor(double? mark) => "#" + GradeColorHelper.GetColor(mark).Substring(3);

    private static double GpaPoints(double mark) => mark switch
    {
        >= 90 => 4.0,
        >= 80 => 3.0,
        >= 70 => 2.0,
        >= 60 => 1.0,
        _ => 0.0
    };

    private static string SchoolLabel(double avg) => avg switch
    {
        >= 95 => "Outstanding",
        >= 90 => "Excellent",
        >= 80 => "Great work",
        >= 70 => "Good",
        >= 60 => "Fair",
        > 0 => "Keep going",
        _ => "—"
    };

    private static void AppendTile(StringBuilder sb, string label, string value, string color)
    {
        sb.Append("<div class='tile'><div class='tile-label'>").Append(label)
          .Append("</div><div class='tile-value' style='color:").Append(color).Append(";'>").Append(value).Append("</div></div>");
    }

    private static void AppendLegend(StringBuilder sb, string range, string argb)
    {
        sb.Append("<span class='legend-item'><span class='swatch' style='background:#").Append(argb.Substring(3)).Append(";'></span>").Append(range).Append("</span>");
    }

    private static string Escape(string? s) => (s ?? string.Empty)
        .Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");

    private const string Css = @"
:root { --ink:#0f172a; --muted:#64748b; --line:#e2e8f0; --bg:#f1f5f9; --card:#ffffff; --indigo:#6366f1; }
* { box-sizing:border-box; }
body { font-family:'Segoe UI',Inter,system-ui,Arial,sans-serif; background:var(--bg); color:var(--ink); margin:0; padding:32px; -webkit-print-color-adjust:exact; print-color-adjust:exact; }
.page { max-width:880px; margin:0 auto; }
.hero { background:linear-gradient(135deg,#312e81,#4c1d95 55%,#6366f1); color:#fff; border-radius:18px; padding:28px 32px; box-shadow:0 18px 40px -18px rgba(49,46,129,.55); overflow:hidden; }
.hero-top { display:flex; justify-content:space-between; align-items:flex-start; gap:16px; }
.brand { display:flex; gap:14px; align-items:center; }
.logo { font-size:34px; line-height:1; }
.brand-title { font-size:18px; font-weight:700; letter-spacing:.2px; }
.brand-sub { font-size:12px; opacity:.75; text-transform:uppercase; letter-spacing:1.5px; }
.hero-meta { text-align:right; }
.student { font-size:17px; font-weight:600; }
.timestamp { font-size:12px; opacity:.7; margin-top:4px; }
.hero-avg { margin-top:26px; display:flex; align-items:baseline; gap:16px; flex-wrap:wrap; }
.avg-label { font-size:12px; text-transform:uppercase; letter-spacing:1.5px; opacity:.8; }
.avg-value { font-size:64px; font-weight:800; line-height:1; }
.avg-value .pct { font-size:30px; font-weight:700; opacity:.85; margin-left:2px; }
.avg-note { font-size:14px; opacity:.9; padding:5px 12px; border:1px solid rgba(255,255,255,.4); border-radius:999px; }
.summary { display:grid; grid-template-columns:repeat(4,1fr); gap:12px; margin:18px 0; }
.tile { background:var(--card); border:1px solid var(--line); border-radius:12px; padding:16px; text-align:center; }
.tile-label { font-size:11px; text-transform:uppercase; letter-spacing:1px; color:var(--muted); }
.tile-value { font-size:26px; font-weight:800; margin-top:6px; }
.courses h2 { font-size:14px; text-transform:uppercase; letter-spacing:1.2px; color:var(--muted); margin:24px 4px 12px; }
.course { display:flex; align-items:center; gap:18px; background:var(--card); border:1px solid var(--line); border-radius:12px; padding:16px 18px; margin-bottom:10px; }
.badge { min-width:64px; text-align:center; font-size:18px; font-weight:800; color:#fff; padding:10px 12px; border-radius:10px; box-shadow:0 6px 14px -8px rgba(0,0,0,.4); }
.course-main { flex:1; min-width:0; }
.course-code { font-size:15px; font-weight:700; }
.course-name { font-size:13px; color:var(--muted); margin:2px 0 6px; }
.course-meta { display:flex; gap:14px; flex-wrap:wrap; font-size:12px; color:var(--muted); }
.course-meta .status { color:#b45309; font-weight:600; }
.legend { display:flex; gap:18px; flex-wrap:wrap; align-items:center; margin:26px 0 8px; padding:14px 18px; background:var(--card); border:1px dashed var(--line); border-radius:12px; }
.legend-title { font-size:11px; text-transform:uppercase; letter-spacing:1px; color:var(--muted); }
.legend-item { display:flex; align-items:center; gap:7px; font-size:12px; color:var(--ink); }
.swatch { width:13px; height:13px; border-radius:4px; display:inline-block; }
footer { text-align:center; font-size:11px; color:var(--muted); margin-top:24px; line-height:1.6; }
@media print {
  body { background:#fff; padding:0; }
  .page { max-width:none; }
  .no-print { display:none; }
  .hero { box-shadow:none; border-radius:0; }
  .course, .tile { break-inside:avoid; }
}
";
}
