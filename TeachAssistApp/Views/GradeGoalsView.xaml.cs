using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using TeachAssistApp.ViewModels;
using Wpf.Ui.Controls;

namespace TeachAssistApp.Views;

public partial class GradeGoalsView : FluentWindow
{
    public GradeGoalsView(IServiceProvider serviceProvider)
    {
        InitializeComponent();
        var viewModel = serviceProvider.GetRequiredService<GradeGoalsViewModel>();
        viewModel.RequestClose += (s, e) => Close();
        DataContext = viewModel;
    }
}
