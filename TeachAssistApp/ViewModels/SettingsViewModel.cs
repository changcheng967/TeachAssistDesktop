using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using TeachAssistApp.Models;
using TeachAssistApp.Services;
using TeachAssistApp.Helpers;
using TeachAssistApp.Views;
using Microsoft.Extensions.DependencyInjection;

namespace TeachAssistApp.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ITeachAssistService _teachAssistService;
    private readonly ICredentialService _credentialService;
    private readonly INavigationService _navigationService;
    private readonly IServiceProvider _serviceProvider;
    private readonly PdfExporter _pdfExporter;

    [ObservableProperty]
    private string _appVersion = "2.0.0";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private string? _successMessage;

    [ObservableProperty]
    private bool _isDarkMode = true;

    [ObservableProperty]
    private bool _enableNotifications = true;

    [ObservableProperty]
    private int _autoRefreshIndex = 0;

    [ObservableProperty]
    private string _autoRefreshInterval = "Off";

    [ObservableProperty]
    private string _username = "Student";

    private List<Course>? _cachedCourses;

    public SettingsViewModel(
        ITeachAssistService teachAssistService,
        ICredentialService credentialService,
        INavigationService navigationService,
        IServiceProvider serviceProvider,
        PdfExporter pdfExporter)
    {
        _teachAssistService = teachAssistService;
        _credentialService = credentialService;
        _navigationService = navigationService;
        _serviceProvider = serviceProvider;
        _pdfExporter = pdfExporter;

        LoadUserData();
    }

    private async void LoadUserData()
    {
        try
        {
            var creds = await _credentialService.GetCredentialsAsync();
            if (!string.IsNullOrEmpty(creds.username))
            {
                Username = creds.username;
            }
        }
        catch { }
    }

    partial void OnIsDarkModeChanged(bool value)
    {
        // Theme switching logic could be implemented here
    }

    partial void OnAutoRefreshIndexChanged(int value)
    {
        AutoRefreshInterval = value switch
        {
            0 => "Off",
            1 => "5",
            2 => "10",
            3 => "15",
            4 => "30",
            _ => "Off"
        };
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        SuccessMessage = null;

        try
        {
            await _teachAssistService.LogoutAsync();
            await _credentialService.ClearCredentialsAsync();
            _navigationService.NavigateTo("Login");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to logout: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ClearCacheAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        SuccessMessage = null;

        try
        {
            await _credentialService.ClearCredentialsAsync();
            SuccessMessage = "✅ Cached data cleared successfully!";
            await Task.Delay(2000);
            SuccessMessage = null;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to clear cache: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void GoBack()
    {
        _navigationService.NavigateTo("Dashboard");
    }

    [RelayCommand]
    private async Task RefreshGradesAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        SuccessMessage = null;

        try
        {
            var courses = await _teachAssistService.GetCoursesAsync();
            _cachedCourses = courses.ToList();
            SuccessMessage = $"✅ Refreshed {courses.Count()} courses!";
            await Task.Delay(2000);
            SuccessMessage = null;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to refresh: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ExportToCsvAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        SuccessMessage = null;

        try
        {
            var courses = _cachedCourses ?? await _teachAssistService.GetCoursesAsync();
            _cachedCourses = courses.ToList();

            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
                DefaultExt = "csv",
                FileName = $"TeachAssist_Grades_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                await ExportToCsvAsync(courses, saveFileDialog.FileName);
                SuccessMessage = $"✅ Exported to {Path.GetFileName(saveFileDialog.FileName)}";
                await Task.Delay(3000);
                SuccessMessage = null;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to export: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ExportToPdfAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        SuccessMessage = null;

        try
        {
            var courses = _cachedCourses ?? await _teachAssistService.GetCoursesAsync();
            _cachedCourses = courses.ToList();

            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "HTML Files (*.html)|*.html|PDF Files (*.pdf)|*.pdf|All Files (*.*)|*.*",
                DefaultExt = "html",
                FileName = $"TeachAssist_GradeReport_{DateTime.Now:yyyyMMdd_HHmmss}.html"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                var html = await _pdfExporter.GenerateGradeReportHtmlAsync(courses, Username);
                var filePath = saveFileDialog.FileName;

                // If user selected .pdf extension, save as .html but let them know
                if (Path.GetExtension(filePath).ToLower() == ".pdf")
                {
                    filePath = Path.ChangeExtension(filePath, ".html");
                    SuccessMessage = "💡 Report opened in browser. Use Ctrl+P to save as PDF!";
                }
                else
                {
                    SuccessMessage = $"✅ Report generated: {Path.GetFileName(filePath)}";
                }

                await _pdfExporter.SaveAndOpenPdfAsync(html, filePath);

                await Task.Delay(3000);
                SuccessMessage = null;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to export: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ViewTrendsAsync()
    {
        try
        {
            var window = new GradeTrendsView(_serviceProvider);
            window.ShowDialog();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to open trends: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task WhatIfCalculatorAsync()
    {
        try
        {
            var window = new WhatIfCalculatorView(_serviceProvider);
            window.ShowDialog();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to open calculator: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task GradeGoalsAsync()
    {
        try
        {
            var window = new GradeGoalsView(_serviceProvider);
            window.ShowDialog();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to open grade goals: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task CheckUpdatesAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        SuccessMessage = null;

        try
        {
            var courses = await _teachAssistService.GetCoursesAsync();
            var coursesWithMarks = courses.Where(c => c.HasValidMark).ToList();

            SuccessMessage = $"🔔 Checked! You have {coursesWithMarks.Count} graded courses. Average: {coursesWithMarks.Average(c => c.NumericMark ?? 0):F1}%";
            await Task.Delay(3000);
            SuccessMessage = null;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to check for updates: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ExportToCsvAsync(IEnumerable<Course> courses, string filePath)
    {
        await using var writer = new StreamWriter(filePath);
        await writer.WriteLineAsync("Course Code,Course Name,Mark,Grade Letter,Level,Room,Block");

        foreach (var course in courses.OrderBy(c => c.Code))
        {
            var mark = course.NumericMark.HasValue ? $"{course.NumericMark.Value:F1}" : "N/A";
            await writer.WriteLineAsync($"\"{course.Code}\",\"{course.Name ?? "N/A"}\",{mark},{course.GradeLetter},{course.GradeLevel},\"{course.Room ?? "N/A"}\",\"{course.Block}\"");
        }
    }
}
