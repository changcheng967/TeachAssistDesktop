using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using TeachAssistApp.ViewModels;
using Wpf.Ui.Controls;

namespace TeachAssistApp.Views;

public partial class WhatIfCalculatorView : FluentWindow
{
    public WhatIfCalculatorView(IServiceProvider serviceProvider)
    {
        InitializeComponent();
        Owner = Application.Current.MainWindow;
        var viewModel = serviceProvider.GetRequiredService<WhatIfCalculatorViewModel>();
        viewModel.RequestClose += (s, e) => Close();
        DataContext = viewModel;
    }
}
