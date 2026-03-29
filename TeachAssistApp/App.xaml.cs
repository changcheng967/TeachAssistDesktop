using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TeachAssistApp.Services;
using TeachAssistApp.ViewModels;
using TeachAssistApp.Views;
using TeachAssistApp.Helpers;
using Wpf.Ui.Appearance;

namespace TeachAssistApp;

public partial class App : Application
{
    private IHost? _host;
    private static readonly string LogDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "TeachAssistApp");
    private static readonly string LogFile = Path.Combine(LogDir, "error.log");

    public IServiceProvider? Services => _host?.Services;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Ensure log directory exists
        try { Directory.CreateDirectory(LogDir); } catch { }

        // Catch UI thread unhandled exceptions with full details
        DispatcherUnhandledException += (sender, args) =>
        {
            var ex = args.Exception;
            var message = FormatException(ex);
            LogError("UI Unhandled", ex);
            System.Windows.MessageBox.Show(
                $"{message}\n\nLog saved to: {LogFile}",
                "TeachAssist Error",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            args.Handled = true;
        };

        // Catch non-UI thread exceptions
        AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
        {
            var ex = args.ExceptionObject as Exception;
            LogError("AppDomain Unhandled", ex);
        };

        // Catch unobserved task exceptions
        TaskScheduler.UnobservedTaskException += (sender, args) =>
        {
            LogError("UnobservedTask", args.Exception);
            args.SetObserved();
        };

        // Apply dark theme by default
        try
        {
            ApplicationThemeManager.Apply(ApplicationTheme.Dark);
        }
        catch
        {
            // Theme detection is non-critical
        }

        try
        {
            _host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    // Services
                    services.AddSingleton<ITeachAssistService, TeachAssistService>();
                    services.AddSingleton<ICredentialService, CredentialService>();
                    services.AddSingleton<ICourseCacheService, CourseCacheService>();
                    services.AddSingleton<Helpers.INavigationService, Helpers.NavigationService>();
                    services.AddSingleton<PdfExporter>();

                    // ViewModels
                    services.AddSingleton<MainViewModel>();
                    services.AddSingleton<LoginViewModel>();
                    services.AddSingleton<DashboardViewModel>();
                    services.AddSingleton<CourseDetailViewModel>();
                    services.AddSingleton<SettingsViewModel>();
                    services.AddSingleton<WhatIfCalculatorViewModel>();
                    services.AddSingleton<GradeTrendsViewModel>();
                    services.AddSingleton<GradeGoalsViewModel>();

                    // Views
                    services.AddSingleton<MainWindow>();
                    services.AddSingleton<LoginView>();
                    services.AddSingleton<DashboardView>();
                    services.AddSingleton<CourseDetailView>();
                    services.AddSingleton<SettingsView>();
                })
                .Build();
        }
        catch (Exception ex)
        {
            LogError("DI build failed", ex);
            System.Windows.MessageBox.Show(
                $"Failed to initialize app services:\n\n{FormatException(ex)}\n\nLog: {LogFile}",
                "TeachAssist Startup Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            Shutdown();
            return;
        }

        try
        {
            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();

            // Watch for system theme changes (must be called after Show)
            try
            {
                SystemThemeWatcher.Watch(mainWindow as Wpf.Ui.Controls.FluentWindow);
            }
            catch { }

            // Initialize overlay brush to match current theme
            UpdateOverlayBrush(IsDarkTheme());
        }
        catch (Exception ex)
        {
            LogError("Window creation failed", ex);
            System.Windows.MessageBox.Show(
                $"Failed to create main window:\n\n{FormatException(ex)}\n\nLog: {LogFile}",
                "TeachAssist Startup Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            Shutdown();
        }
    }

    private static string FormatException(Exception? ex)
    {
        if (ex == null) return "Unknown error (null exception)";
        var lines = new System.Text.StringBuilder();
        lines.AppendLine($"{ex.GetType().Name}: {ex.Message}");
        if (ex.StackTrace != null)
        {
            var frames = ex.StackTrace.Split('\n');
            for (int i = 0; i < Math.Min(5, frames.Length); i++)
                lines.AppendLine(frames[i].Trim());
        }
        if (ex.InnerException != null)
        {
            lines.AppendLine($"\nInner: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
            if (ex.InnerException.StackTrace != null)
            {
                var frames = ex.InnerException.StackTrace.Split('\n');
                for (int i = 0; i < Math.Min(3, frames.Length); i++)
                    lines.AppendLine(frames[i].Trim());
            }
        }
        return lines.ToString();
    }

    public static void LogError(string context, Exception? ex)
    {
        try
        {
            var entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{context}]\n{FormatException(ex)}\n{new string('-', 60)}\n";
            File.AppendAllText(LogFile, entry);
        }
        catch { }
    }

    public static bool IsDarkTheme()
    {
        try
        {
            var bgBrush = Current.Resources["ApplicationPageBackgroundThemeBrush"] as SolidColorBrush;
            if (bgBrush != null)
            {
                // Dark backgrounds have low R values
                return bgBrush.Color.R < 128;
            }
        }
        catch { }
        return true; // default to dark
    }

    public static void UpdateOverlayBrush(bool isDark)
    {
        // Replace the entire brush instead of modifying Color (XAML brushes may be frozen)
        Current.Resources["OverlayBrush"] = new SolidColorBrush(
            isDark
                ? Color.FromArgb(153, 0, 0, 0)
                : Color.FromArgb(153, 245, 245, 244));
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _host?.Dispose();
        base.OnExit(e);
    }
}

// Value Converters
public class StringToBoolConverter : System.Windows.Data.IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return !string.IsNullOrEmpty(value as string);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class BooleanToVisibilityConverter : System.Windows.Data.IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Visibility visibility)
        {
            return visibility == Visibility.Visible;
        }
        return false;
    }
}

public class InvertedBooleanToVisibilityConverter : System.Windows.Data.IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue ? Visibility.Collapsed : Visibility.Visible;
        }
        return Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Visibility visibility)
        {
            return visibility != Visibility.Visible;
        }
        return true;
    }
}

public class StringToBrushConverter : System.Windows.Data.IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var color = value as string;
        if (string.IsNullOrEmpty(color)) return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Gray);

        try
        {
            return new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(color));
        }
        catch
        {
            return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Gray);
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class MarkToColorConverter : System.Windows.Data.IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var mark = value as double?;
        var colorHex = GradeColorHelper.GetColor(mark);
        return new System.Windows.Media.SolidColorBrush(
            (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(colorHex));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

// Additional converters for CourseDetailView
public class StringToVisibilityConverter : System.Windows.Data.IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string stringValue && !string.IsNullOrWhiteSpace(stringValue))
        {
            return Visibility.Visible;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class EmptyCollectionToVisibilityConverter : System.Windows.Data.IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is System.Collections.ICollection collection && collection.Count == 0)
        {
            return Visibility.Visible;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class PercentageConverter : System.Windows.Data.IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length >= 2 && values[0] is double percentage && values[1] is double totalWidth)
        {
            return totalWidth * (percentage / 100.0);
        }
        return 0.0;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class ImpactDeltaToColorConverter : System.Windows.Data.IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double delta)
        {
            return delta >= 0
                ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(35, 134, 54))   // #238636 green
                : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(248, 81, 73));    // #F85149 red
        }
        return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class NullToVisibilityConverter : System.Windows.Data.IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value != null ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class IntZeroToBoolConverter : System.Windows.Data.IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int count)
            return count == 0;
        return true;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
