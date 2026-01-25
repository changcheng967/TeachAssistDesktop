using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using TeachAssistApp.Helpers;

namespace TeachAssistApp.Views;

public partial class DashboardView : Page
{
    public DashboardView()
    {
        InitializeComponent();
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        if (Application.Current is App app)
        {
            var navigationService = app.Services?.GetService<INavigationService>();
            navigationService?.NavigateTo("Settings");
        }
    }

    private void QuickActions_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.ContextMenu is ContextMenu menu)
        {
            menu.PlacementTarget = button;
            menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            menu.IsOpen = true;
        }
    }
}
