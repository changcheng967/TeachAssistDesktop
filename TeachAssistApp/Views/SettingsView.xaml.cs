using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace TeachAssistApp.Views;

public partial class SettingsView : Page
{
    private static readonly CubicEase EaseOut = new() { EasingMode = EasingMode.EaseOut };

    public SettingsView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is ViewModels.SettingsViewModel oldVm)
            oldVm.PropertyChanged -= OnLoadingChanged;

        if (e.NewValue is ViewModels.SettingsViewModel newVm)
            newVm.PropertyChanged += OnLoadingChanged;
    }

    private void OnLoadingChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModels.SettingsViewModel.IsLoading) && DataContext is ViewModels.SettingsViewModel vm)
        {
            Dispatcher.Invoke(() =>
            {
                // Find the loading overlay grid
                foreach (var child in LogicalTreeHelper.GetChildren(this))
                {
                    if (child is Grid g && g.Background is System.Windows.Media.SolidColorBrush brush
                        && brush.Color.A > 0 && brush.Color.R == 0 && brush.Color.G == 0 && brush.Color.B == 0)
                    {
                        if (vm.IsLoading)
                        {
                            g.Visibility = Visibility.Visible;
                            g.Opacity = 0;
                            var fadeIn = new DoubleAnimation
                            {
                                From = 0, To = 1,
                                Duration = TimeSpan.FromMilliseconds(200),
                                EasingFunction = EaseOut
                            };
                            g.BeginAnimation(UIElement.OpacityProperty, fadeIn);
                        }
                        else
                        {
                            var fadeOut = new DoubleAnimation
                            {
                                From = 1, To = 0,
                                Duration = TimeSpan.FromMilliseconds(300),
                                EasingFunction = EaseOut
                            };
                            fadeOut.Completed += (s, _) =>
                            {
                                g.Visibility = Visibility.Collapsed;
                                g.Opacity = 1;
                            };
                            g.BeginAnimation(UIElement.OpacityProperty, fadeOut);
                        }
                        break;
                    }
                }
            });
        }
    }

    private void ExecuteCommand(string commandName)
    {
        if (DataContext is ViewModels.SettingsViewModel vm)
        {
            switch (commandName)
            {
                case "Trends": vm.ViewTrendsCommand.Execute(null); break;
                case "Goals": vm.GradeGoalsCommand.Execute(null); break;
                case "WhatIf": vm.WhatIfCalculatorCommand.Execute(null); break;
                case "Update": vm.CheckForUpdatesCommand.Execute(null); break;
                case "Csv": vm.ExportToCsvCommand.Execute(null); break;
                case "Pdf": vm.ExportToPdfCommand.Execute(null); break;
                case "Clear": vm.ClearCacheCommand.Execute(null); break;
            }
        }
    }

    private void ToolTrends_Click(object sender, MouseButtonEventArgs e) => ExecuteCommand("Trends");
    private void ToolGoals_Click(object sender, MouseButtonEventArgs e) => ExecuteCommand("Goals");
    private void ToolWhatIf_Click(object sender, MouseButtonEventArgs e) => ExecuteCommand("WhatIf");
    private void ToolUpdate_Click(object sender, MouseButtonEventArgs e) => ExecuteCommand("Update");
    private void ToolCsv_Click(object sender, MouseButtonEventArgs e) => ExecuteCommand("Csv");
    private void ToolPdf_Click(object sender, MouseButtonEventArgs e) => ExecuteCommand("Pdf");
    private void DataClear_Click(object sender, MouseButtonEventArgs e) => ExecuteCommand("Clear");
}
