using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using TeachAssistApp.ViewModels;

namespace TeachAssistApp.Views;

public partial class GradeTrendsView : Window
{
    public GradeTrendsView(IServiceProvider serviceProvider)
    {
        InitializeComponent();
        var viewModel = serviceProvider.GetRequiredService<GradeTrendsViewModel>();
        viewModel.RequestClose += (s, e) => Close();
        DataContext = viewModel;
    }
}
