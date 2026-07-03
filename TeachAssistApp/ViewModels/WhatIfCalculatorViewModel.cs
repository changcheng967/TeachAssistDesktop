using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using TeachAssistApp.Services;
using TeachAssistApp.Helpers;
using TeachAssistApp.Models;

namespace TeachAssistApp.ViewModels;

/// <summary>
/// Course-aware What-If simulator. The student picks a course, then adds hypothetical
/// assignments (mark %, category, size in points) and sees how that course's weighted
/// grade — and their overall average — would change. Grade math is delegated to
/// <see cref="GradeImpactCalculator"/>, the same engine CourseDetailView uses, so the
/// projection is consistent with the grades shown everywhere else in the app.
/// </summary>
public partial class WhatIfCalculatorViewModel : ObservableObject
{
    private readonly ITeachAssistService _teachAssistService;

    private List<Models.Course> _allCourses = new();
    private Course? _loadedCourse;
    private List<AssignmentGroup> _baseGroups = new();
    private WeightTable _weightTable = new();

    // Transcript baseline computed consistently with the projection (see Recalculate).
    private double _othersSum;
    private int _transcriptCount;

    [ObservableProperty] private ObservableCollection<Course> _courses = new();
    [ObservableProperty] private Course? _selectedCourse;

    // Transcript summary (matches the Dashboard's portal-based average).
    [ObservableProperty] private string _gradeColor = GradeColorHelper.NA;
    [ObservableProperty] private double _currentAverage;
    [ObservableProperty] private double _baselineAverage;
    [ObservableProperty] private int _courseCount;

    // Selected-course state.
    [ObservableProperty] private bool _hasCourseData;
    [ObservableProperty] private bool _isLoadingCourse;
    [ObservableProperty] private double _currentCourseGrade;
    [ObservableProperty] private string _currentCourseGradeColor = GradeColorHelper.NA;

    // Projection results.
    [ObservableProperty] private double _projectedCourseGrade;
    [ObservableProperty] private double _courseGradeDifference;
    [ObservableProperty] private double _projectedAverage;
    [ObservableProperty] private double _averageDifference;
    [ObservableProperty] private bool _hasHypotheticalAssignments;

    // Hypothetical inputs.
    [ObservableProperty] private string _newAssignmentName = string.Empty;
    [ObservableProperty] private string _newAssignmentMark = string.Empty;
    [ObservableProperty] private string _newAssignmentPoints = string.Empty;
    [ObservableProperty] private ObservableCollection<string> _categories = new();
    [ObservableProperty] private string? _selectedCategory;
    [ObservableProperty] private ObservableCollection<HypotheticalAssignment> _hypotheticalAssignments = new();

    public event EventHandler? RequestClose;

    public WhatIfCalculatorViewModel(ITeachAssistService teachAssistService)
    {
        _teachAssistService = teachAssistService;
        _ = LoadCurrentDataAsync();
    }

    private async Task LoadCurrentDataAsync()
    {
        try
        {
            _allCourses = (await _teachAssistService.GetCoursesAsync())
                .Where(c => !c.IsLunch)
                .ToList();
            Courses = new ObservableCollection<Course>(_allCourses);

            var valid = _allCourses.Where(c => c.HasValidMark).ToList();
            CourseCount = valid.Count;
            CurrentAverage = valid.Any() ? valid.Average(c => c.NumericMark ?? 0) : 0;
            BaselineAverage = CurrentAverage;
            GradeColor = GradeColorHelper.GetColor(CurrentAverage);
        }
        catch (Exception ex)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"WhatIf load error: {ex.Message}");
#endif
        }
    }

    partial void OnSelectedCourseChanged(Course? value)
    {
        // Reset projection state whenever the simulated course changes.
        HypotheticalAssignments.Clear();
        HasHypotheticalAssignments = false;
        HasCourseData = false;
        Categories.Clear();
        SelectedCategory = null;
        ProjectedCourseGrade = 0;
        CourseGradeDifference = 0;
        ProjectedAverage = CurrentAverage;
        AverageDifference = 0;

        if (value == null) return;
        _ = LoadCourseDetailAsync(value);
    }

    private async Task LoadCourseDetailAsync(Course course)
    {
        IsLoadingCourse = true;
        try
        {
            _loadedCourse = course;

            var detail = (!string.IsNullOrEmpty(course.SubjectId) && !string.IsNullOrEmpty(course.StudentId))
                ? await _teachAssistService.GetCourseDetailsAsync(course.SubjectId, course.StudentId)
                : null;

            var assignments = (detail != null && detail.Assignments.Count > 0)
                ? detail.Assignments
                : course.Assignments;
            _weightTable = (detail != null && detail.WeightTable.Weights.Count > 0)
                ? detail.WeightTable
                : course.WeightTable;

            _baseGroups = assignments
                .GroupBy(a => a.Name)
                .Select(g => new AssignmentGroup
                {
                    Name = g.Key,
                    Assignments = new ObservableCollection<Assignment>(g)
                })
                .ToList();

            CurrentCourseGrade = ComputeCourseGrade(_baseGroups);
            CurrentCourseGradeColor = GradeColorHelper.GetColor(CurrentCourseGrade);
            ProjectedCourseGrade = CurrentCourseGrade;
            CourseGradeDifference = 0;

            // Transcript baseline that uses the app-computed course grade for the selected
            // course, so the projection delta reflects ONLY the hypothetical assignments.
            // BaselineAverage is shown explicitly so the before→after is never hidden.
            RecomputeTranscriptBaseline();
            BaselineAverage = _transcriptCount > 0
                ? (_othersSum + CurrentCourseGrade) / _transcriptCount
                : CurrentAverage;
            ProjectedAverage = BaselineAverage;
            AverageDifference = 0;

            // Category picker: prefer the course's real weighted strands, else the standard set.
            var cats = _weightTable.Weights.Count > 0
                ? _weightTable.Weights.Keys.ToList()
                : new List<string> { "KU", "T", "C", "A" };
            var order = new[] { "KU", "T", "C", "A", "F", "O" };
            cats = cats
                .OrderBy(c => { var i = Array.IndexOf(order, c); return i < 0 ? 99 : i; })
                .ThenBy(c => c)
                .ToList();
            Categories = new ObservableCollection<string>(cats);
            SelectedCategory = cats.FirstOrDefault() ?? "KU";

            HasCourseData = true;
        }
        catch (Exception ex)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"WhatIf course detail error: {ex.Message}");
#endif
        }
        finally
        {
            IsLoadingCourse = false;
        }
    }

    private double ComputeCourseGrade(List<AssignmentGroup> groups)
    {
        if (groups.Count == 0)
        {
            // No assignment detail available — fall back to the course's posted mark.
            return _loadedCourse?.NumericMark ?? 0;
        }
        var (timeline, _) = GradeImpactCalculator.Calculate(groups, _weightTable);
        return timeline.Count > 0 ? timeline.Last().CumulativeGrade : (_loadedCourse?.NumericMark ?? 0);
    }

    private void RecomputeTranscriptBaseline()
    {
        if (_loadedCourse == null)
        {
            _othersSum = 0;
            _transcriptCount = 0;
            return;
        }
        var others = _allCourses
            .Where(c => c.HasValidMark && !ReferenceEquals(c, _loadedCourse))
            .ToList();
        _othersSum = others.Sum(c => c.NumericMark ?? 0);
        _transcriptCount = others.Count + 1; // +1 for the simulated course
    }

    [RelayCommand]
    private void AddAssignment()
    {
        if (!HasCourseData) return;
        if (!double.TryParse(NewAssignmentMark, out var markPct)) return;
        if (!double.TryParse(NewAssignmentPoints, out var points) || points <= 0) return;

        var name = string.IsNullOrWhiteSpace(NewAssignmentName)
            ? $"Hypothetical {HypotheticalAssignments.Count + 1}"
            : NewAssignmentName;
        var category = string.IsNullOrEmpty(SelectedCategory) ? "KU" : SelectedCategory;

        // Remove by reference so deleting a non-tail item can never misroute (previous
        // implementation captured the index at creation time).
        var item = new HypotheticalAssignment
        {
            Name = name,
            Mark = markPct,
            Category = category,
            Points = points
        };
        item.RemoveCommand = new RelayCommand(() => RemoveAssignment(item));
        HypotheticalAssignments.Add(item);

        NewAssignmentName = string.Empty;
        NewAssignmentMark = string.Empty;
        NewAssignmentPoints = string.Empty;

        Recalculate();
    }

    private void RemoveAssignment(HypotheticalAssignment item)
    {
        HypotheticalAssignments.Remove(item);
        Recalculate();
    }

    private void Recalculate()
    {
        HasHypotheticalAssignments = HypotheticalAssignments.Any();

        if (!HasCourseData)
        {
            ProjectedCourseGrade = CurrentCourseGrade;
            CourseGradeDifference = 0;
            ProjectedAverage = CurrentAverage;
            AverageDifference = 0;
            return;
        }

        // Rebuild the assignment list with each hypothetical appended as its own group.
        var working = _baseGroups.ToList();
        foreach (var h in HypotheticalAssignments)
        {
            var achieved = h.Mark / 100.0 * h.Points; // mark% of the point total
            working.Add(new AssignmentGroup
            {
                Name = h.Name,
                Assignments = new ObservableCollection<Assignment>
                {
                    new Assignment
                    {
                        Name = h.Name,
                        Category = h.Category,
                        MarkAchieved = achieved,
                        MarkPossible = h.Points
                    }
                }
            });
        }

        ProjectedCourseGrade = ComputeCourseGrade(working);
        CourseGradeDifference = ProjectedCourseGrade - CurrentCourseGrade;

        // Transcript impact: only the selected course's grade changes between scenarios,
        // so the delta is measured against the displayed BaselineAverage.
        if (_transcriptCount > 0)
        {
            ProjectedAverage = (_othersSum + ProjectedCourseGrade) / _transcriptCount;
            AverageDifference = ProjectedAverage - BaselineAverage;
        }
        else
        {
            ProjectedAverage = CurrentAverage;
            AverageDifference = 0;
        }
    }

    [RelayCommand]
    private void Close()
    {
        RequestClose?.Invoke(this, EventArgs.Empty);
    }
}

public class HypotheticalAssignment
{
    public string Name { get; set; } = string.Empty;
    public double Mark { get; set; }
    public string Category { get; set; } = "KU";
    public double Points { get; set; }
    public IRelayCommand? RemoveCommand { get; set; }
}
