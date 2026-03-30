using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using TeachAssistApp.ViewModels;
using Wpf.Ui.Controls;

namespace TeachAssistApp.Views;

public partial class GradeGoalsView : FluentWindow
{
    public GradeGoalsView(IServiceProvider serviceProvider)
    {
        InitializeComponent();
        Owner = Application.Current.MainWindow;
        var viewModel = serviceProvider.GetRequiredService<GradeGoalsViewModel>();
        viewModel.RequestClose += (s, e) => Close();
        DataContext = viewModel;
    }

    private void PresetGoal_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is double percent && DataContext is GradeGoalsViewModel vm)
        {
            vm.SetGoalCommand.Execute(percent);
        }
    }
}
