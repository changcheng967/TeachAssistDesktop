using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using TeachAssistApp.Models;
using TeachAssistApp.Services;
using TeachAssistApp.Helpers;
using TeachAssistApp.Views;
using Microsoft.Extensions.DependencyInjection;
using Wpf.Ui.Appearance;

namespace TeachAssistApp.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ITeachAssistService _teachAssistService;
    private readonly ICredentialService _credentialService;
    private readonly INavigationService _navigationService;
    private readonly IServiceProvider _serviceProvider;
    private readonly PdfExporter _pdfExporter;

    [ObservableProperty]
    private string _appVersion = "5.2.0";

    [ObservableProperty]
    private string _updateStatus = string.Empty;

    [ObservableProperty]
    private bool _isCheckingForUpdates;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private string? _successMessage;

    [ObservableProperty]
    private bool _isDarkMode;

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

        // Detect current theme instead of always defaulting to dark
        _isDarkMode = App.IsDarkTheme();

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

    public async Task RefreshUserDataAsync()
    {
        try
        {
            var creds = await _credentialService.GetCredentialsAsync();
            if (!string.IsNullOrEmpty(creds.username))
            {
                Username = creds.username;
            }
            else
            {
                Username = "Student";
            }
        }
        catch
        {
            Username = "Student";
        }
    }

    partial void OnIsDarkModeChanged(bool value)
    {
        try
        {
            Wpf.Ui.Appearance.ApplicationThemeManager.Apply(
                value ? Wpf.Ui.Appearance.ApplicationTheme.Dark : Wpf.Ui.Appearance.ApplicationTheme.Light);
            App.UpdateOverlayBrush(value);
        }
        catch { }
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
            Username = "Student";
            _cachedCourses = null;
            UpdateStatus = string.Empty;
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
            _teachAssistService.ClearCache();
            _cachedCourses = null;
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
    private async Task CheckForUpdatesAsync()
    {
        IsCheckingForUpdates = true;
        UpdateStatus = "Checking for updates...";
        ErrorMessage = null;
        SuccessMessage = null;

        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "TeachAssistDesktop");
            var response = await client.GetStringAsync("https://api.github.com/repos/changcheng967/TeachAssistDesktop/releases/latest");
            var json = JsonDocument.Parse(response);
            var latestTag = json.RootElement.GetProperty("tag_name").GetString();

            var current = new Version(AppVersion);
            var latestTagStr = json.RootElement.GetProperty("tag_name").GetString();
            if (string.IsNullOrEmpty(latestTagStr)) { UpdateStatus = "Could not determine latest version."; return; }
            var latest = new Version(latestTagStr.TrimStart('v'));

            if (latest > current)
            {
                UpdateStatus = $"Update available: v{latest}! Opening Microsoft Store...";
                SuccessMessage = $"New version v{latest} available. Opening Microsoft Store...";
                // Open Microsoft Store page for TeachAssist Desktop
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "ms-windows-store://pdp/?ProductId=9P6CSMJZJT14",
                        UseShellExecute = true
                    });
                }
                catch
                {
                    // Fallback: open in browser
                    try
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "https://apps.microsoft.com/detail/9P6CSMJZJT14",
                            UseShellExecute = true
                        });
                    }
                    catch { }
                }
            }
            else
            {
                UpdateStatus = "You're on the latest version.";
                SuccessMessage = "You're on the latest version!";
                await Task.Delay(2000);
                SuccessMessage = null;
            }
        }
        catch (Exception ex)
        {
            UpdateStatus = "Could not check for updates.";
            ErrorMessage = $"Update check failed: {ex.Message}";
        }
        finally
        {
            IsCheckingForUpdates = false;
        }
    }

    [RelayCommand]
    private async Task RefreshSummaryAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        SuccessMessage = null;

        try
        {
            var courses = await _teachAssistService.GetCoursesAsync();
            var coursesWithMarks = courses.Where(c => c.HasValidMark).ToList();

            if (coursesWithMarks.Count > 0)
                SuccessMessage = $"Refreshed! You have {coursesWithMarks.Count} graded courses. Average: {coursesWithMarks.Average(c => c.NumericMark ?? 0):F1}%";
            else
                SuccessMessage = "Refreshed! No graded courses found.";
            await Task.Delay(3000);
            SuccessMessage = null;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to refresh summary: {ex.Message}";
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
