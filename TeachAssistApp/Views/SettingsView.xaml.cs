using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace TeachAssistApp.Views;

public partial class SettingsView : Page
{
    public SettingsView()
    {
        InitializeComponent();
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
