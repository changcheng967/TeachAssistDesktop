using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using TeachAssistApp.ViewModels;
using Wpf.Ui.Controls;

namespace TeachAssistApp.Views;

public partial class GradeTrendsView : FluentWindow
{
    public GradeTrendsView(IServiceProvider serviceProvider)
    {
        InitializeComponent();
        var viewModel = serviceProvider.GetRequiredService<GradeTrendsViewModel>();
        viewModel.RequestClose += (s, e) => Close();
        DataContext = viewModel;
    }
}
