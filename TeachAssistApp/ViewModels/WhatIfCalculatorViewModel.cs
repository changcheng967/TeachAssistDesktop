using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using TeachAssistApp.Services;

namespace TeachAssistApp.ViewModels;

public partial class WhatIfCalculatorViewModel : ObservableObject
{
    private readonly ITeachAssistService _teachAssistService;
    private List<Models.Course>? _cachedCourses;

    [ObservableProperty]
    private double _currentGpa;

    [ObservableProperty]
    private int _courseCount;

    [ObservableProperty]
    private double _projectedGpa;

    [ObservableProperty]
    private double _gpaDifference;

    [ObservableProperty]
    private bool _hasHypotheticalAssignments;

    [ObservableProperty]
    private string _newAssignmentName = string.Empty;

    [ObservableProperty]
    private string _newAssignmentMark = string.Empty;

    [ObservableProperty]
    private string _newAssignmentWeight = string.Empty;

    [ObservableProperty]
    private ObservableCollection<HypotheticalAssignment> _hypotheticalAssignments = new();

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
            var courses = await _teachAssistService.GetCoursesAsync();
            _cachedCourses = courses.ToList();

            // Calculate current GPA (only courses with valid marks)
            var validCourses = _cachedCourses.Where(c => c.HasValidMark).ToList();
            if (validCourses.Any())
            {
                CurrentGpa = validCourses.Average(c => c.NumericMark ?? 0);
                CourseCount = validCourses.Count;
            }
            else
            {
                CurrentGpa = 0;
                CourseCount = 0;
            }

            UpdateProjection();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading courses: {ex.Message}");
        }
    }

    [RelayCommand]
    private void AddAssignment()
    {
        if (!double.TryParse(NewAssignmentMark, out var mark) ||
            !double.TryParse(NewAssignmentWeight, out var weight))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(NewAssignmentName))
        {
            NewAssignmentName = $"Assignment {HypotheticalAssignments.Count + 1}";
        }

        HypotheticalAssignments.Add(new HypotheticalAssignment
        {
            Name = NewAssignmentName,
            Mark = mark,
            Weight = weight,
            RemoveCommand = new RelayCommand(() => RemoveAssignment(mark, weight))
        });

        // Clear inputs
        NewAssignmentName = string.Empty;
        NewAssignmentMark = string.Empty;
        NewAssignmentWeight = string.Empty;

        UpdateProjection();
    }

    private void RemoveAssignment(double mark, double weight)
    {
        var toRemove = HypotheticalAssignments.FirstOrDefault(a => a.Mark == mark && a.Weight == weight);
        if (toRemove != null)
        {
            HypotheticalAssignments.Remove(toRemove);
            UpdateProjection();
        }
    }

    private void UpdateProjection()
    {
        HasHypotheticalAssignments = HypotheticalAssignments.Any();

        if (_cachedCourses == null || !_cachedCourses.Any())
        {
            ProjectedGpa = 0;
            GpaDifference = 0;
            return;
        }

        // Calculate projected GPA with hypothetical assignments
        var validCourses = _cachedCourses.Where(c => c.HasValidMark).ToList();
        var totalWeight = validCourses.Count * 100.0; // Each course counts as 100%
        var weightedSum = validCourses.Sum(c => (c.NumericMark ?? 0) * 100.0);

        // Add hypothetical assignments
        foreach (var hypo in HypotheticalAssignments)
        {
            weightedSum += hypo.Mark * hypo.Weight;
            totalWeight += hypo.Weight;
        }

        if (totalWeight > 0)
        {
            ProjectedGpa = weightedSum / totalWeight;
            GpaDifference = ProjectedGpa - CurrentGpa;
        }
        else
        {
            ProjectedGpa = 0;
            GpaDifference = 0;
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
    public double Weight { get; set; }
    public IRelayCommand? RemoveCommand { get; set; }
}
