using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace TeachAssistApp.Helpers;

public static class Windows11Helper
{
    #region Windows 11 Mica & Window Effects

    [DllImport("user32.dll")]
    private static extern int SetWindowAttribute(IntPtr hwnd, int dwAttribute, int dwAttributeSize, ref int pvAttribute);

    [DllImport("user32.dll")]
    private static extern int GetWindowLongPtr(IntPtr hWnd, int nIndex);

    private const int WCA_EXCLUDED_FROM_LIVEPREVIEW = 14;
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const int DWMWA_MICA_EFFECT = 1029;
    private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;

    // Corner preferences
    private const int DWMWCP_DEFAULT = 0;
    private const int DWMWCP_DONOTROUND = 1;
    private const int DWMWCP_ROUND = 2;
    private const int DWMWCP_ROUNDSMALL = 3;

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    public static void EnableWindows11Effects(Window window)
    {
        try
        {
            var hwnd = new WindowInteropHelper(window).EnsureHandle();
            var helper = new WindowInteropHelper(window);

            // Enable Mica effect (Windows 11 22H2+)
            int mica = 1;
            DwmSetWindowAttribute(hwnd, DWMWA_MICA_EFFECT, ref mica, sizeof(int));

            // Set corner preference to round (Windows 11 style)
            int cornerPreference = DWMWCP_ROUND;
            DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref cornerPreference, sizeof(int));

            // Enable immersive dark mode for title bar
            int darkMode = 1;
            DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int));

            System.Diagnostics.Debug.WriteLine("Windows 11 effects enabled");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to enable Windows 11 effects: {ex.Message}");
        }
    }

    public static void SetImmersiveDarkMode(Window window, bool darkMode)
    {
        try
        {
            var hwnd = new WindowInteropHelper(window).EnsureHandle();
            int mode = darkMode ? 1 : 0;
            DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref mode, sizeof(int));
        }
        catch { }
    }

    #endregion

    #region Windows Toast Notifications

    public static void ShowGradeNotification(string courseName, double oldGrade, double newGrade)
    {
        try
        {
            // Create Windows Toast Notification
            var toastXml = $@"
                <toast activationType='protocol' launch='teachassist://{courseName}' scenario='default'>
                    <visual>
                        <binding template='ToastGeneric'>
                            <text>Grade Update!</text>
                            <text>{courseName}</text>
                            <text>{oldGrade:F1}% → {newGrade:F1}%</text>
                            <text placement='attribution'>TeachAssist</text>
                        </binding>
                    </visual>
                    <audio src='ms-winsoundevent:Notification.Default'/>
                </toast>";

            // For now, show a simple message box - full toast requires Windows.UI.Notifications
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                MessageBox.Show(
                    $"{courseName}: {oldGrade:F1}% → {newGrade:F1}%",
                    "Grade Updated!",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to show notification: {ex.Message}");
        }
    }

    #endregion

    #region Windows 11 Snap Layouts

    public static void ApplySnapLayoutHints(Window window)
    {
        // Windows 11 automatically shows snap layouts for windows
        // We just need to ensure our window is properly sized
        window.Width = 1200;
        window.Height = 800;
        window.MinWidth = 900;
        window.MinHeight = 600;
        window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
    }

    #endregion

    #region System Theme Detection

    public static bool IsWindowsDarkTheme()
    {
        try
        {
            var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            if (key != null)
            {
                var value = key.GetValue("AppsUseLightTheme");
                if (value != null && value is int intValue)
                {
                    return intValue == 0;
                }
            }
        }
        catch { }
        return false; // Default to light
    }

    #endregion

    #region Windows Version Detection

    public static bool IsWindows11()
    {
        try
        {
            var version = Environment.OSVersion.Version;
            return version.Major >= 10 && version.Build >= 22000;
        }
        catch { }
        return false;
    }

    public static bool IsWindows11OrGreater()
    {
        return IsWindows11();
    }

    #endregion
}
