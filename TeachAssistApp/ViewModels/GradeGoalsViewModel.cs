using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using TeachAssistApp.Services;

namespace TeachAssistApp.ViewModels;

public partial class GradeGoalsViewModel : ObservableObject
{
    private readonly ITeachAssistService _teachAssistService;

    [ObservableProperty]
    private double _currentAverage;

    [ObservableProperty]
    private double _goalPercent = 85;

    [ObservableProperty]
    private double _progressToGoal;

    [ObservableProperty]
    private double _pointsNeeded;

    [ObservableProperty]
    private bool _onTrack;

    [ObservableProperty]
    private string _customGoal = string.Empty;

    [ObservableProperty]
    private string _motivationalMessage = string.Empty;

    [ObservableProperty]
    private bool _showMotivation;

    [ObservableProperty]
    private ObservableCollection<PresetGoal> _presetGoals = new();

    public event EventHandler? RequestClose;

    public GradeGoalsViewModel(ITeachAssistService teachAssistService)
    {
        _teachAssistService = teachAssistService;

        // Initialize preset goals
        InitializePresetGoals();

        // Load current data
        _ = LoadDataAsync();
    }

    private void InitializePresetGoals()
    {
        PresetGoals.Add(new PresetGoal { Percent = 90, Label = "Excellent", Color = "#FF238636" });
        PresetGoals.Add(new PresetGoal { Percent = 85, Label = "Very Good", Color = "#FF3FB950" });
        PresetGoals.Add(new PresetGoal { Percent = 80, Label = "Good", Color = "#FFD29922" });
        PresetGoals.Add(new PresetGoal { Percent = 75, Label = "Satisfactory", Color = "#FFDB6D28" });
        PresetGoals.Add(new PresetGoal { Percent = 70, Label = "Passing", Color = "#FFA57104" });
    }

    private async Task LoadDataAsync()
    {
        try
        {
            var courses = await _teachAssistService.GetCoursesAsync();
            var validCourses = courses.Where(c => c.HasValidMark).ToList();

            if (validCourses.Any())
            {
                CurrentAverage = validCourses.Average(c => c.NumericMark ?? 0);

                // Load saved goal
                var savedGoal = LoadSavedGoal();
                if (savedGoal > 0)
                {
                    GoalPercent = savedGoal;
                }

                CalculateProgress();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading data: {ex.Message}");
        }
    }

    private void CalculateProgress()
    {
        ProgressToGoal = (CurrentAverage / GoalPercent) * 100;
        PointsNeeded = GoalPercent - CurrentAverage;
        OnTrack = CurrentAverage >= GoalPercent * 0.9; // Within 90% of goal

        UpdateMotivationalMessage();
    }

    private void UpdateMotivationalMessage()
    {
        if (OnTrack)
        {
            if (CurrentAverage >= GoalPercent)
            {
                MotivationalMessage = "🎉 Congratulations! You've reached your goal! Keep up the amazing work!";
            }
            else
            {
                MotivationalMessage = "💪 You're so close to your goal! Just a little more effort and you'll make it!";
            }
            ShowMotivation = true;
        }
        else if (PointsNeeded > 15)
        {
            MotivationalMessage = "📚 Every assignment counts. Focus on your studies and aim high!";
            ShowMotivation = true;
        }
        else
        {
            ShowMotivation = false;
        }
    }

    [RelayCommand]
    private void SetGoal(double percent)
    {
        GoalPercent = percent;
        CalculateProgress();
        SaveGoal(percent);
    }

    [RelayCommand]
    private void SetCustomGoal()
    {
        if (double.TryParse(CustomGoal, out var goal) && goal >= 0 && goal <= 100)
        {
            GoalPercent = goal;
            CalculateProgress();
            SaveGoal(goal);
        }
    }

    private void SaveGoal(double goal)
    {
        // Save to local settings
        try
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var settingsPath = System.IO.Path.Combine(appData, "TeachAssistApp", "goals.txt");
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(settingsPath) ?? string.Empty);
            System.IO.File.WriteAllText(settingsPath, goal.ToString());
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving goal: {ex.Message}");
        }
    }

    private double LoadSavedGoal()
    {
        try
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var settingsPath = System.IO.Path.Combine(appData, "TeachAssistApp", "goals.txt");

            if (System.IO.File.Exists(settingsPath))
            {
                var content = System.IO.File.ReadAllText(settingsPath);
                if (double.TryParse(content, out var saved))
                {
                    return saved;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading goal: {ex.Message}");
        }
        return 85; // Default goal
    }

    [RelayCommand]
    private void Close()
    {
        RequestClose?.Invoke(this, EventArgs.Empty);
    }
}

public class PresetGoal
{
    public double Percent { get; set; }
    public string Label { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
}
