using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Microsoft.Extensions.DependencyInjection;
using TeachAssistApp.Helpers;
using TeachAssistApp.ViewModels;
using TeachAssistApp.Views;

namespace TeachAssistApp;

public partial class MainWindow : Window
{
    private readonly IServiceProvider _serviceProvider;
    private readonly INavigationService _navigationService;

    public MainWindow(IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _serviceProvider = serviceProvider;
        _navigationService = serviceProvider.GetRequiredService<INavigationService>();

        // Subscribe to navigation events
        _navigationService.OnNavigate += OnNavigate;

        // Enable Windows 11 effects
        if (Windows11Helper.IsWindows11OrGreater())
        {
            Windows11Helper.EnableWindows11Effects(this);
            Windows11Helper.ApplySnapLayoutHints(this);
        }

        // Add keyboard shortcuts
        this.KeyDown += MainWindow_KeyDown;

        // Navigate to login page initially
        NavigateTo("Login");
    }

    private void MainWindow_KeyDown(object sender, KeyEventArgs e)
    {
        // F5 - Refresh
        if (e.Key == Key.F5)
        {
            HandleRefresh();
            e.Handled = true;
        }
        // Ctrl + , - Settings
        else if (e.Key == Key.OemComma && Keyboard.Modifiers == ModifierKeys.Control)
        {
            _navigationService.NavigateTo("Settings");
            e.Handled = true;
        }
        // Ctrl + E - Export
        else if (e.Key == Key.E && Keyboard.Modifiers == ModifierKeys.Control)
        {
            HandleExport();
            e.Handled = true;
        }
        // Escape - Go back to Dashboard
        else if (e.Key == Key.Escape)
        {
            var currentPage = ContentFrame.Content;
            if (currentPage != null && currentPage.GetType().Name != "DashboardView")
            {
                _navigationService.NavigateTo("Dashboard");
                e.Handled = true;
            }
        }
    }

    private void HandleRefresh()
    {
        var currentPage = ContentFrame.Content;
        if (currentPage is DashboardView)
        {
            var viewModel = _serviceProvider.GetRequiredService<DashboardViewModel>();
            viewModel.LoadCoursesCommand.Execute(null);
        }
        else if (currentPage is CourseDetailView)
        {
            var viewModel = _serviceProvider.GetRequiredService<CourseDetailViewModel>();
            // Refresh current course
        }
    }

    private async void HandleExport()
    {
        var currentPage = ContentFrame.Content;
        if (currentPage is DashboardView)
        {
            var settingsViewModel = _serviceProvider.GetRequiredService<SettingsViewModel>();
            await settingsViewModel.ExportToCsvCommand.ExecuteAsync(null);
        }
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // Apply fade-in animation on load
        var fadeIn = (Storyboard)FindResource("WindowFadeIn");
        fadeIn.Begin(this);
    }

    private void OnNavigate(string viewName)
    {
        NavigateTo(viewName);
    }

    private void NavigateTo(string viewName)
    {
        System.Diagnostics.Debug.WriteLine($"NavigateTo called: {viewName}");
        Page? page = null;

        // Parse view name with optional parameters (e.g., "CourseDetail|ICS4U1-03")
        var parts = viewName.Split('|');
        var baseView = parts[0];
        System.Diagnostics.Debug.WriteLine($"  Base view: {baseView}");
        if (parts.Length > 1)
        {
            System.Diagnostics.Debug.WriteLine($"  Parameter: {parts[1]}");
        }

        switch (baseView)
        {
            case "Login":
                page = _serviceProvider.GetRequiredService<LoginView>();
                var loginViewModel = _serviceProvider.GetRequiredService<LoginViewModel>();
                // Reload credentials (will clear them after logout)
                _ = loginViewModel.LoadSavedCredentialsAsync();
                page.DataContext = loginViewModel;
                break;
            case "Dashboard":
                page = _serviceProvider.GetRequiredService<DashboardView>();
                page.DataContext = _serviceProvider.GetRequiredService<DashboardViewModel>();
                break;
            case "Settings":
                page = _serviceProvider.GetRequiredService<SettingsView>();
                page.DataContext = _serviceProvider.GetRequiredService<SettingsViewModel>();
                break;
            case "CourseDetail":
                if (parts.Length > 1)
                {
                    var courseCode = parts[1];
                    System.Diagnostics.Debug.WriteLine($"  Loading CourseDetailView for: {courseCode}");
                    page = _serviceProvider.GetRequiredService<CourseDetailView>();
                    var viewModel = _serviceProvider.GetRequiredService<CourseDetailViewModel>();
                    viewModel.LoadCourse(courseCode);
                    page.DataContext = viewModel;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"  Loading CourseDetailView without course code");
                    page = _serviceProvider.GetRequiredService<CourseDetailView>();
                    page.DataContext = _serviceProvider.GetRequiredService<CourseDetailViewModel>();
                }
                break;
        }

        if (page != null)
        {
            System.Diagnostics.Debug.WriteLine($"  Navigating to page");
            // First navigation or empty Frame: skip animation, navigate instantly
            if (ContentFrame.Content == null)
            {
                try
                {
                    ContentFrame.Navigate(page);
                    System.Diagnostics.Debug.WriteLine($"  Instant navigation (first load)");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"  Instant navigation failed: {ex.Message}");
                }
            }
            else
            {
                try
                {
                    AnimateTransition(page, baseView == "CourseDetail");
                }
                catch
                {
                    ContentFrame.Navigate(page);
                }
            }
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"  Page is null, navigation failed");
        }
    }

    private void AnimateTransition(Page newPage, bool isForward)
    {
        // Simple fade-out then navigate and fade-in
        var fadeOut = new DoubleAnimation
        {
            From = 1,
            To = 0,
            Duration = TimeSpan.FromMilliseconds(200),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };

        var outSb = new Storyboard();
        Storyboard.SetTarget(fadeOut, ContentFrame);
        Storyboard.SetTargetProperty(fadeOut, new PropertyPath(UIElement.OpacityProperty));
        outSb.Children.Add(fadeOut);

        outSb.Completed += (_, _) =>
        {
            Dispatcher.Invoke(() =>
            {
                try
                {
                    ContentFrame.Navigate(newPage);
                    ContentFrame.Opacity = 0;

                    var fadeIn = new DoubleAnimation
                    {
                        From = 0,
                        To = 1,
                        Duration = TimeSpan.FromMilliseconds(300),
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                    };
                    var inSb = new Storyboard();
                    Storyboard.SetTarget(fadeIn, ContentFrame);
                    Storyboard.SetTargetProperty(fadeIn, new PropertyPath(UIElement.OpacityProperty));
                    inSb.Children.Add(fadeIn);
                    inSb.Begin(this);
                }
                catch
                {
                    // Fallback: navigate without animation
                }
            });
        };

        outSb.Begin(this);
    }

    #region Window Control Events

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            // Double click to maximize/restore
            Maximize_Click(sender, e);
        }
        else
        {
            // Drag to move
            if (WindowState == WindowState.Maximized)
            {
                // When maximized, restore to normal and move
                var point = Mouse.GetPosition(this);
                WindowState = WindowState.Normal;
                // Adjust position to keep mouse relative to window
                Left = point.X - (RestoreBounds.Width / 2);
                Top = point.Y;
            }

            DragMove();
        }
    }

    private void Minimize_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void Maximize_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    #endregion

    protected override void OnClosed(EventArgs e)
    {
        _navigationService.OnNavigate -= OnNavigate;
        base.OnClosed(e);
    }
}
