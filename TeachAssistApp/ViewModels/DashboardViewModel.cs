using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using TeachAssistApp.Services;
using TeachAssistApp.Models;
using TeachAssistApp.Helpers;
using IServiceProvider = System.IServiceProvider;

namespace TeachAssistApp.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly ITeachAssistService _teachAssistService;
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private ObservableCollection<Course> _courses = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private string _gpa = "N/A";

    [ObservableProperty]
    private double _averageMark = 0;

    [ObservableProperty]
    private int _courseCount;

    [ObservableProperty]
    private bool _isEmpty;

    [ObservableProperty]
    private string _schoolName = "YRDSB";

    [ObservableProperty]
    private string _gradeLabel = "";

    [ObservableProperty]
    private string _gradeColor = GradeColorHelper.NA;

    private readonly IServiceProvider _serviceProvider;

    public DashboardViewModel(
        ITeachAssistService teachAssistService,
        INavigationService navigationService,
        IServiceProvider serviceProvider)
    {
        _teachAssistService = teachAssistService;
        _navigationService = navigationService;
        _serviceProvider = serviceProvider;

        // Initially showing loading, not empty
        IsEmpty = false;

        // Don't load courses in constructor — they may not be available yet.
        // NavigationService.NavigateTo("Dashboard") triggers the load via LoadCoursesCommand.
    }

    [RelayCommand]
    private async Task LoadCoursesAsync()
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var courses = await _teachAssistService.GetCoursesAsync();

            Courses.Clear();
            foreach (var course in courses)
            {
                Courses.Add(course);
            }

            // Show service error if no courses returned
            if (Courses.Count == 0)
            {
                ErrorMessage = _teachAssistService.LastError ?? "No courses found. Check your credentials and try again.";
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"No courses loaded. Error: {_teachAssistService.LastError}");
#endif
            }

            CalculateStatistics();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load courses: {ex.Message}";
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"LoadCoursesAsync exception: {ex}");
#endif
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void SelectCourse(Course? course)
    {
        if (course != null && !course.IsLunch)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"SelectCourse called: {course.Code}");
            System.Diagnostics.Debug.WriteLine($"  SubjectId: {course.SubjectId}");
            System.Diagnostics.Debug.WriteLine($"  StudentId: {course.StudentId}");
            System.Diagnostics.Debug.WriteLine($"  Assignments count: {course.Assignments.Count}");
#endif

            // Navigate to course detail
            _navigationService.NavigateTo($"CourseDetail|{course.Code}");
        }
    }

    private void CalculateStatistics()
    {
        // Check if empty (no courses)
        IsEmpty = Courses.Count == 0 && !IsLoading;

        // Update school name from service
        SchoolName = _teachAssistService.SchoolName;

        var academicCourses = Courses.Where(c => !c.IsLunch);
        var validCourses = academicCourses.Where(c => c.HasValidMark).ToList();

        if (validCourses.Any())
        {
            var sum = validCourses.Sum(c => c.NumericMark ?? 0);
            AverageMark = sum / validCourses.Count;

            GradeColor = GradeColorHelper.GetColor(AverageMark);
            GradeLabel = AverageMark switch
            {
                >= 95 => "Outstanding",
                >= 90 => "Excellent",
                >= 80 => "Great",
                >= 70 => "Good",
                >= 60 => "Fair",
                > 0 => "Keep Going",
                _ => ""
            };

            var gpaSum = validCourses.Sum(c =>
            {
                var mark = c.NumericMark ?? 0;
                if (mark >= 90) return 4.0;
                if (mark >= 80) return 3.0;
                if (mark >= 70) return 2.0;
                if (mark >= 60) return 1.0;
                return 0.0;
            });
            Gpa = (gpaSum / validCourses.Count).ToString("F2");
        }
        else
        {
            AverageMark = 0;
            GradeColor = GradeColorHelper.NA;
            GradeLabel = "";
            Gpa = "N/A";
        }

        CourseCount = academicCourses.Count();
    }

    [RelayCommand]
    private void ViewTrends()
    {
        var window = new Views.GradeTrendsView(_serviceProvider);
        window.ShowDialog();
    }

    [RelayCommand]
    private void WhatIf()
    {
        var window = new Views.WhatIfCalculatorView(_serviceProvider);
        window.ShowDialog();
    }

    [RelayCommand]
    private void GradeGoals()
    {
        var window = new Views.GradeGoalsView(_serviceProvider);
        window.ShowDialog();
    }

    [RelayCommand]
    private async Task Export()
    {
        try
        {
            var pdfExporter = new Services.PdfExporter();
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "HTML Files (*.html)|*.html|PDF Files (*.pdf)|*.pdf|All Files (*.*)|*.*",
                DefaultExt = "html",
                FileName = $"TeachAssist_Grades_{System.DateTime.Now:yyyyMMdd_HHmmss}.html"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                var html = await pdfExporter.GenerateGradeReportHtmlAsync(Courses.ToList(), "Student");
                var filePath = saveFileDialog.FileName;

                if (System.IO.Path.GetExtension(filePath).ToLower() == ".pdf")
                {
                    filePath = System.IO.Path.ChangeExtension(filePath, ".html");
                }

                await pdfExporter.SaveAndOpenPdfAsync(html, filePath);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Export failed: {ex.Message}";
        }
    }
}
