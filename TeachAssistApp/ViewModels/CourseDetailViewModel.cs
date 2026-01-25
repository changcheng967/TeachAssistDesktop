using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using TeachAssistApp.Services;
using TeachAssistApp.Models;
using TeachAssistApp.Helpers;

namespace TeachAssistApp.ViewModels;

public partial class CourseDetailViewModel : ObservableObject
{
    private readonly ITeachAssistService _teachAssistService;
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private Course? _selectedCourse;

    [ObservableProperty]
    private ObservableCollection<AssignmentGroup> _assignmentGroups = new();

    [ObservableProperty]
    private ObservableCollection<CategoryPerformance> _categoryPerformance = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _hasTrends;

    [ObservableProperty]
    private ObservableCollection<AssignmentTrendDisplay> _assignmentTrendsDisplay = new();

    public CourseDetailViewModel(
        ITeachAssistService teachAssistService,
        INavigationService navigationService)
    {
        _teachAssistService = teachAssistService;
        _navigationService = navigationService;
    }

    public async void LoadCourse(string courseCode)
    {
        System.Diagnostics.Debug.WriteLine($"LoadCourse called with: {courseCode}");
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            // Get the course from cache
            var courses = await _teachAssistService.GetCoursesAsync();
            SelectedCourse = courses.FirstOrDefault(c => c.Code == courseCode);

            if (SelectedCourse != null)
            {
                System.Diagnostics.Debug.WriteLine($"Found course: {SelectedCourse.Code}");
                System.Diagnostics.Debug.WriteLine($"  SubjectId: {SelectedCourse.SubjectId}");
                System.Diagnostics.Debug.WriteLine($"  StudentId: {SelectedCourse.StudentId}");

                // Fetch detailed assignment data from TeachAssist API
                if (!string.IsNullOrEmpty(SelectedCourse.SubjectId) && !string.IsNullOrEmpty(SelectedCourse.StudentId))
                {
                    System.Diagnostics.Debug.WriteLine($"Fetching course details from API...");
                    var detailedCourse = await _teachAssistService.GetCourseDetailsAsync(
                        SelectedCourse.SubjectId,
                        SelectedCourse.StudentId);

                    if (detailedCourse != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"API returned {detailedCourse.Assignments.Count} assignments");
                        if (detailedCourse.Assignments.Count > 0)
                        {
                            SelectedCourse.Assignments = detailedCourse.Assignments;
                            SelectedCourse.WeightTable = detailedCourse.WeightTable;
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"API returned null course");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Skipping API fetch - SubjectId or StudentId is null");
                }

                // Group assignments by name
                AssignmentGroups.Clear();
                var groupedAssignments = SelectedCourse.Assignments
                    .GroupBy(a => a.Name)
                    .Select(g => new AssignmentGroup
                    {
                        Name = g.Key,
                        Assignments = new ObservableCollection<Assignment>(g)
                    });

                foreach (var group in groupedAssignments)
                {
                    AssignmentGroups.Add(group);
                }

                System.Diagnostics.Debug.WriteLine($"Loaded {AssignmentGroups.Count} assignment groups into UI");

                // Calculate category performance
                CalculateCategoryPerformance();
                System.Diagnostics.Debug.WriteLine($"Calculated {CategoryPerformance.Count} category performance items");

                // Populate trends for visualization
                PopulateTrends();
                System.Diagnostics.Debug.WriteLine($"Loaded {AssignmentTrendsDisplay.Count} trends for visualization");
            }
            else
            {
                ErrorMessage = "Course not found.";
                System.Diagnostics.Debug.WriteLine($"Course not found: {courseCode}");
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load course: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"LoadCourse error: {ex}");
        }
        finally
        {
            IsLoading = false;
            System.Diagnostics.Debug.WriteLine($"LoadCourse complete. IsLoading={IsLoading}");
        }
    }

    private void CalculateCategoryPerformance()
    {
        CategoryPerformance.Clear();

        if (SelectedCourse == null) return;

        var categories = new[] { "KU", "T", "C", "A", "F", "O" };
        var categoryNames = new Dictionary<string, string>
        {
            { "KU", "Knowledge/Understanding" },
            { "T", "Thinking" },
            { "C", "Communication" },
            { "A", "Application" },
            { "F", "Final" },
            { "O", "Other" }
        };

        foreach (var category in categories)
        {
            var categoryAssignments = SelectedCourse.Assignments.Where(a => a.Category == category).ToList();

            if (categoryAssignments.Any())
            {
                // Filter out assignments with null marks
                var validAssignments = categoryAssignments.Where(a => a.MarkAchieved.HasValue && a.MarkPossible.HasValue).ToList();

                if (validAssignments.Any())
                {
                    var totalAchieved = validAssignments.Sum(a => a.MarkAchieved!.Value);
                    var totalPossible = validAssignments.Sum(a => a.MarkPossible!.Value);
                    var percentage = totalPossible > 0 ? (totalAchieved / totalPossible) * 100 : 0;

                    CategoryPerformance.Add(new CategoryPerformance
                    {
                        Code = category,
                        Name = categoryNames[category],
                        Percentage = Math.Round(percentage, 1),
                        Weight = SelectedCourse.WeightTable.GetWeight(category) ?? 0.0,
                        AssignmentCount = categoryAssignments.Count
                    });
                }
            }
        }
    }

    private void PopulateTrends()
    {
        AssignmentTrendsDisplay.Clear();

        if (SelectedCourse == null)
        {
            HasTrends = false;
            return;
        }

        // Check if course has trend data (CGC format or regular assignments)
        if (SelectedCourse.IsCGCFormat && SelectedCourse.AssignmentTrends.Any())
        {
            // Use CGC trend data
            HasTrends = true;

            // Show most recent 15 assignments
            foreach (var t in SelectedCourse.AssignmentTrends.Take(15))
            {
                AssignmentTrendsDisplay.Add(new AssignmentTrendDisplay
                {
                    AssignmentName = t.AssignmentName,
                    Mark = t.Mark,
                    Weight = t.Weight,
                    TrendColor = GetTrendColor(t.Mark)
                });
            }
        }
        else if (SelectedCourse.Assignments.Any())
        {
            // Create trend data from regular assignments
            HasTrends = true;

            // Group assignments by name and calculate average mark
            var assignmentGroups = SelectedCourse.Assignments
                .Where(a => a.MarkAchieved.HasValue && a.MarkPossible.HasValue && a.MarkPossible.Value > 0)
                .GroupBy(a => a.Name)
                .Select(g => new
                {
                    Name = g.Key,
                    AverageMark = g.Average(a => (a.MarkAchieved!.Value / a.MarkPossible!.Value) * 100),
                    TotalWeight = g.Sum(a => a.Weight ?? 0)
                })
                .OrderBy(a => a.Name)
                .Take(15);

            foreach (var assignment in assignmentGroups)
            {
                AssignmentTrendsDisplay.Add(new AssignmentTrendDisplay
                {
                    AssignmentName = assignment.Name,
                    Mark = Math.Round(assignment.AverageMark, 1),
                    Weight = assignment.TotalWeight,
                    TrendColor = GetTrendColor(assignment.AverageMark)
                });
            }
        }
        else
        {
            HasTrends = false;
        }
    }

    private string GetTrendColor(double mark)
    {
        if (mark >= 90) return "#FF238636"; // Excellent - Green
        if (mark >= 80) return "#FFD29922"; // Good - Gold/Yellow
        return "#FFDB6D28"; // Needs Improvement - Orange
    }

    [RelayCommand]
    private void GoBack()
    {
        _navigationService.NavigateTo("Dashboard");
    }
}

public class CategoryPerformance
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public double Percentage { get; set; }
    public double Weight { get; set; }
    public int AssignmentCount { get; set; }

    public string GradeColor
    {
        get
        {
            if (Percentage >= 95) return "#FF2EA043";
            if (Percentage >= 90) return "#FF3FB950";
            if (Percentage >= 85) return "#FF238636";
            if (Percentage >= 80) return "#FFD29922";
            if (Percentage >= 75) return "#FF9A6700";
            if (Percentage >= 70) return "#FFDB6D28";
            if (Percentage >= 65) return "#FFA57104";
            if (Percentage >= 60) return "#FFf85149";
            return "#FFD73A49";
        }
    }
}

public class AssignmentTrendDisplay
{
    public string AssignmentName { get; set; } = string.Empty;
    public double Mark { get; set; }
    public double Weight { get; set; }
    public string TrendColor { get; set; } = "#FF238636";
}
