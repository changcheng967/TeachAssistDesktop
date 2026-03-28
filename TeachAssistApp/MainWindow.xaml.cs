using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using TeachAssistApp.Helpers;
using TeachAssistApp.Services;
using TeachAssistApp.ViewModels;
using TeachAssistApp.Views;
using Wpf.Ui.Controls;

namespace TeachAssistApp;

public partial class MainWindow : FluentWindow
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Helpers.INavigationService _navigationService;
    private string _currentView = "";
    private NavigationViewItem? _dashboardItem;
    private NavigationViewItem? _settingsItem;
    private string? _pendingNavigation;

    public MainWindow(IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _serviceProvider = serviceProvider;
        _navigationService = serviceProvider.GetRequiredService<Helpers.INavigationService>();

        _navigationService.OnNavigate += OnNavigate;
        this.KeyDown += MainWindow_KeyDown;

        // Attempt auto-login with saved credentials; fall back to Login page
        _pendingNavigation = "Login";

        Loaded += async (_, _) =>
        {
            var credentialService = _serviceProvider.GetRequiredService<ICredentialService>();
            var (username, password) = await credentialService.GetCredentialsAsync();
            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                var teachAssistService = _serviceProvider.GetRequiredService<ITeachAssistService>();
                var success = await teachAssistService.LoginAsync(username, password);
                if (success)
                {
                    _navigationService.NavigateTo("Dashboard");
                }
            }
        };
    }

    private void MainWindow_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F5)
        {
            if (_currentView == "Dashboard")
            {
                var viewModel = _serviceProvider.GetRequiredService<DashboardViewModel>();
                viewModel.LoadCoursesCommand.Execute(null);
            }
            e.Handled = true;
        }
        else if (e.Key == Key.OemComma && Keyboard.Modifiers == ModifierKeys.Control)
        {
            _navigationService.NavigateTo("Settings");
            e.Handled = true;
        }
        else if (e.Key == Key.E && Keyboard.Modifiers == ModifierKeys.Control)
        {
            if (_currentView == "Dashboard")
            {
                var settingsViewModel = _serviceProvider.GetRequiredService<SettingsViewModel>();
                _ = settingsViewModel.ExportToCsvCommand.ExecuteAsync(null);
            }
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            if (_currentView != "Dashboard" && _currentView != "Login")
            {
                _navigationService.NavigateTo("Dashboard");
                e.Handled = true;
            }
        }
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        WindowsIntegration.UpdateJumpList([]);

        // Cache nav items and wire click handlers
        foreach (var item in RootNavigation.MenuItems)
        {
            if (item is NavigationViewItem navItem)
            {
                if (navItem.Tag?.ToString() == "Dashboard") _dashboardItem = navItem;
                if (navItem.Tag?.ToString() == "Settings") _settingsItem = navItem;

                navItem.PreviewMouseLeftButtonDown += NavItem_PreviewMouseLeftButtonDown;
            }
        }

        // Process deferred navigation
        if (_pendingNavigation != null)
        {
            NavigateTo(_pendingNavigation);
            _pendingNavigation = null;
        }
    }

    private void NavItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is NavigationViewItem navItem)
        {
            var tag = navItem.Tag?.ToString();
            if (tag != null && tag != _currentView)
            {
                _navigationService.NavigateTo(tag);
            }
        }
    }

    private void OnNavigate(string viewName) => NavigateTo(viewName);

    private void NavigateTo(string viewName)
    {
        Page? page = null;
        var parts = viewName.Split('|');
        var baseView = parts[0];

        switch (baseView)
        {
            case "Login":
                page = _serviceProvider.GetRequiredService<LoginView>();
                var loginViewModel = _serviceProvider.GetRequiredService<LoginViewModel>();
                _ = loginViewModel.LoadSavedCredentialsAsync();
                page.DataContext = loginViewModel;
                RootNavigation.Visibility = Visibility.Collapsed;
                break;
            case "Dashboard":
                page = _serviceProvider.GetRequiredService<DashboardView>();
                page.DataContext = _serviceProvider.GetRequiredService<DashboardViewModel>();
                RootNavigation.Visibility = Visibility.Visible;
                HighlightNavItem(_dashboardItem);
                break;
            case "Settings":
                page = _serviceProvider.GetRequiredService<SettingsView>();
                var settingsViewModel = _serviceProvider.GetRequiredService<SettingsViewModel>();
                page.DataContext = settingsViewModel;
                _ = settingsViewModel.RefreshUserDataAsync();
                RootNavigation.Visibility = Visibility.Visible;
                HighlightNavItem(_settingsItem);
                break;
            case "CourseDetail":
                if (parts.Length > 1)
                {
                    var courseCode = parts[1];
                    page = _serviceProvider.GetRequiredService<CourseDetailView>();
                    var viewModel = _serviceProvider.GetRequiredService<CourseDetailViewModel>();
                    viewModel.LoadCourse(courseCode);
                    page.DataContext = viewModel;
                }
                else
                {
                    page = _serviceProvider.GetRequiredService<CourseDetailView>();
                    page.DataContext = _serviceProvider.GetRequiredService<CourseDetailViewModel>();
                }
                RootNavigation.Visibility = Visibility.Visible;
                break;
        }

        if (page != null)
        {
            _currentView = baseView;
            // Navigate Frame to the page (Frame is the proper host for Page)
            // Remove back entries to prevent unbounded journal growth
            while (ContentArea.CanGoBack)
            {
                ContentArea.RemoveBackEntry();
            }
            ContentArea.Navigate(page);
        }
    }

    private void HighlightNavItem(NavigationViewItem? item)
    {
        foreach (var mi in RootNavigation.MenuItems)
        {
            if (mi is NavigationViewItem nvi) nvi.IsActive = false;
        }
        if (item != null) item.IsActive = true;
    }

    protected override void OnClosed(EventArgs e)
    {
        _navigationService.OnNavigate -= OnNavigate;
        base.OnClosed(e);
    }
}
