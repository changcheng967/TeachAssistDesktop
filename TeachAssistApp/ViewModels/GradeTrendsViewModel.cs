using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using TeachAssistApp.Services;

namespace TeachAssistApp.ViewModels;

public partial class GradeTrendsViewModel : ObservableObject
{
    private readonly ITeachAssistService _teachAssistService;

    [ObservableProperty]
    private double _overallAverage;

    [ObservableProperty]
    private double _highestMark;

    [ObservableProperty]
    private double _lowestMark;

    [ObservableProperty]
    private ObservableCollection<CourseTrendItem> _courseTrends = new();

    [ObservableProperty]
    private ObservableCollection<string> _insights = new();

    public event EventHandler? RequestClose;

    public GradeTrendsViewModel(ITeachAssistService teachAssistService)
    {
        _teachAssistService = teachAssistService;
        _ = LoadTrendsAsync();
    }

    private async Task LoadTrendsAsync()
    {
        try
        {
            var courses = await _teachAssistService.GetCoursesAsync();
            var validCourses = courses.Where(c => c.HasValidMark).ToList();

            if (validCourses.Any())
            {
                OverallAverage = validCourses.Average(c => c.NumericMark ?? 0);
                HighestMark = validCourses.Max(c => c.NumericMark ?? 0);
                LowestMark = validCourses.Min(c => c.NumericMark ?? 0);

                // Create course trend items
                foreach (var course in validCourses.OrderByDescending(c => c.NumericMark))
                {
                    CourseTrends.Add(new CourseTrendItem
                    {
                        Code = course.Code,
                        Name = course.Name,
                        Mark = course.NumericMark ?? 0,
                        GradeColor = course.GradeColor
                    });
                }

                // Generate insights
                GenerateInsights(validCourses);
            }
            else
            {
                OverallAverage = 0;
                HighestMark = 0;
                LowestMark = 0;
                Insights.Add("No course data available yet.");
            }
        }
        catch (Exception ex)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"Error loading trends: {ex.Message}");
#endif
            Insights.Add($"Error loading data: {ex.Message}");
        }
    }

    private void GenerateInsights(List<Models.Course> courses)
    {
        Insights.Clear();

        var avg = OverallAverage;

        if (avg >= 90)
        {
            Insights.Add("🎉 Excellent work! You're maintaining an A average across all courses.");
        }
        else if (avg >= 80)
        {
            Insights.Add("👍 Good job! You're performing well above average.");
        }
        else if (avg >= 70)
        {
            Insights.Add("📚 You're doing okay, but there's room for improvement.");
        }
        else
        {
            Insights.Add("⚠️ Consider focusing on your studies to improve your grades.");
        }

        // Find best and worst courses
        var best = courses.OrderByDescending(c => c.NumericMark ?? 0).First();
        var worst = courses.OrderBy(c => c.NumericMark ?? 0).First();

        Insights.Add($"🏆 Strongest performance: {best.Code} ({best.NumericMark ?? 0:F1}%)");
        Insights.Add($"📝 Area for improvement: {worst.Code} ({worst.NumericMark ?? 0:F1}%)");

        // Check for struggling courses
        var struggling = courses.Where(c => c.NumericMark < 70).ToList();
        if (struggling.Any())
        {
            Insights.Add($"⚠️ {struggling.Count} course(s) below 70% - consider extra help.");
        }

        // Check for excellent courses
        var excellent = courses.Where(c => c.NumericMark >= 90).ToList();
        if (excellent.Any())
        {
            Insights.Add($"⭐ {excellent.Count} course(s) at 90% or higher - outstanding!");
        }
    }

    [RelayCommand]
    private void Close()
    {
        RequestClose?.Invoke(this, EventArgs.Empty);
    }
}

public class CourseTrendItem
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public double Mark { get; set; }
    public string GradeColor { get; set; } = "#FF30363D";
}
