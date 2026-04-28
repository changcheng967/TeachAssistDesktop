using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
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
    private Border? _activeNavItem;
    private string? _pendingNavigation;

    public MainWindow(IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _serviceProvider = serviceProvider;
        _navigationService = serviceProvider.GetRequiredService<Helpers.INavigationService>();

        // Initialize toast notification service
        var toastService = serviceProvider.GetRequiredService<Services.IToastService>() as Services.ToastService;
        toastService?.SetHost(ToastHost);

        _navigationService.OnNavigate += OnNavigate;
        this.KeyDown += MainWindow_KeyDown;

        // Attempt auto-login with saved credentials; fall back to Login page
        _pendingNavigation = "AutoLogin";
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

    private async Task AutoLoginAsync()
    {
        try
        {
            var credentialService = _serviceProvider.GetRequiredService<ICredentialService>();
            var (username, password) = await credentialService.GetCredentialsAsync();
            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                // Show login page with loading state while we attempt login
                NavigateTo("Login");
                var loginViewModel = _serviceProvider.GetRequiredService<LoginViewModel>();
                loginViewModel.IsLoading = true;

                var teachAssistService = _serviceProvider.GetRequiredService<ITeachAssistService>();
                var success = await teachAssistService.LoginAsync(username, password);
                loginViewModel.IsLoading = false;
                if (success)
                {
                    _navigationService.NavigateTo("Dashboard");
                }
                // If login failed, stay on Login page (user can manually re-enter)
            }
            else
            {
                NavigateTo("Login");
            }
        }
        catch
        {
            NavigateTo("Login");
        }
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        WindowsIntegration.UpdateJumpList([]);

        // Process deferred navigation
        if (_pendingNavigation == "AutoLogin")
        {
            _pendingNavigation = null;
            _ = AutoLoginAsync();
        }
        else if (_pendingNavigation != null)
        {
            NavigateTo(_pendingNavigation);
            _pendingNavigation = null;
        }
    }

    private void NavItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border navItem)
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
                Sidebar.Visibility = Visibility.Collapsed;
                AppTitleBar.Margin = new Thickness(0);
                break;
            case "Dashboard":
                page = _serviceProvider.GetRequiredService<DashboardView>();
                var dashboardVM = _serviceProvider.GetRequiredService<DashboardViewModel>();
                page.DataContext = dashboardVM;
                if (dashboardVM.Courses.Count == 0 && !dashboardVM.IsLoading)
                {
                    _ = dashboardVM.LoadCoursesCommand.ExecuteAsync(null);
                }
                Sidebar.Visibility = Visibility.Visible;
                AppTitleBar.Margin = new Thickness(0);
                SetActiveNav(NavDashboard);
                // Update sidebar stat display
                UpdateSidebarGrade(dashboardVM);
                dashboardVM.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(DashboardViewModel.AverageMark) ||
                        e.PropertyName == nameof(DashboardViewModel.GradeColor))
                    {
                        Dispatcher.Invoke(() => UpdateSidebarGrade(dashboardVM));
                    }
                    else if (e.PropertyName == nameof(DashboardViewModel.Gpa))
                    {
                        Dispatcher.Invoke(() =>
                        {
                            SidebarGpa.Text = dashboardVM.Gpa != "N/A" ? $"GPA {dashboardVM.Gpa}" : "";
                        });
                    }
                };
                break;
            case "Settings":
                page = _serviceProvider.GetRequiredService<SettingsView>();
                var settingsViewModel = _serviceProvider.GetRequiredService<SettingsViewModel>();
                page.DataContext = settingsViewModel;
                _ = settingsViewModel.RefreshUserDataAsync();
                Sidebar.Visibility = Visibility.Visible;
                AppTitleBar.Margin = new Thickness(0);
                SetActiveNav(NavSettings);
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
                Sidebar.Visibility = Visibility.Visible;
                AppTitleBar.Margin = new Thickness(0);
                ClearActiveNav();
                break;
        }

        if (page != null)
        {
            _currentView = baseView;
            while (ContentArea.CanGoBack)
                ContentArea.RemoveBackEntry();

            // Fade out current content before navigating
            if (ContentArea.Content is FrameworkElement oldContent)
            {
                var fadeOut = new DoubleAnimation
                {
                    From = 1, To = 0,
                    Duration = TimeSpan.FromMilliseconds(150),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
                };
                fadeOut.Completed += (s, _) => ContentArea.Navigate(page);
                oldContent.BeginAnimation(UIElement.OpacityProperty, fadeOut);
            }
            else
            {
                ContentArea.Navigate(page);
            }
        }
    }

    private void SetActiveNav(Border? item)
    {
        ClearActiveNav();
        if (item == null) return;
        _activeNavItem = item;
        item.Background = new SolidColorBrush(Color.FromArgb(0x20, 0xFF, 0xFF, 0xFF)); // White tint on dark sidebar
    }

    private void UpdateSidebarGrade(DashboardViewModel vm)
    {
        SidebarAverage.Text = vm.AverageMark > 0 ? $"{vm.AverageMark:F1}%" : "--";
        SidebarGpa.Text = vm.Gpa != "N/A" ? $"GPA {vm.Gpa}" : "";

        if (vm.AverageMark > 0 && !string.IsNullOrEmpty(vm.GradeColor))
        {
            var color = (Color)ColorConverter.ConvertFromString(vm.GradeColor);
            SidebarGradeAccent.Background = new SolidColorBrush(color);
            SidebarGradeAccent.Opacity = 0.8;
        }
        else
        {
            SidebarGradeAccent.Opacity = 0;
        }
    }

    private void ClearActiveNav()
    {
        if (_activeNavItem != null)
        {
            _activeNavItem.Background = Brushes.Transparent;
            _activeNavItem = null;
        }
    }

    private void ContentArea_Navigated(object sender, NavigationEventArgs e)
    {
        if (e.Content is FrameworkElement fe)
        {
            fe.Opacity = 0;
            var fadeIn = new DoubleAnimation
            {
                From = 0, To = 1,
                Duration = TimeSpan.FromMilliseconds(400),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            fe.BeginAnimation(UIElement.OpacityProperty, fadeIn);
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        _navigationService.OnNavigate -= OnNavigate;
        base.OnClosed(e);
    }
}
