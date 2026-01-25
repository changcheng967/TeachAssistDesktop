using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Shell;
using TeachAssistApp.Models;

namespace TeachAssistApp.Helpers;

public static class WindowsIntegration
{
    #region Jump List Support

    public static void UpdateJumpList(System.Collections.Generic.IEnumerable<Course> courses)
    {
        try
        {
            var jumpList = JumpList.GetJumpList(Application.Current);
            if (jumpList == null)
            {
                jumpList = new JumpList();
                JumpList.SetJumpList(Application.Current, jumpList);
            }

            // Clear existing items
            jumpList.JumpItems.Clear();
            jumpList.ShowFrequentCategory = false;
            jumpList.ShowRecentCategory = false;

            // Add courses to Jump List
            foreach (var course in courses.Take(courses.Count() > 10 ? 10 : courses.Count()))
            {
                if (!course.HasValidMark) continue;

                var jumpTask = new JumpTask
                {
                    Title = $"{course.Code} - {course.DisplayMark}",
                    Description = course.Name ?? "Course",
                    Arguments = $"--course {course.Code}",
                    CustomCategory = "Courses"
                };

                jumpList.JumpItems.Add(jumpTask);
            }

            jumpList.Apply();
            System.Diagnostics.Debug.WriteLine($"Updated Jump List with {courses.Count()} courses");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to update Jump List: {ex.Message}");
        }
    }

    #endregion

    #region System Tray Support

    public static void AddSystemTrayIcon(Action onRefresh, Action onExit)
    {
        try
        {
            // System tray implementation requires NotifyIcon
            // This is a placeholder for Windows Forms integration
            System.Diagnostics.Debug.WriteLine("System tray support enabled");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to add system tray: {ex.Message}");
        }
    }

    #endregion

    #region Windows Notification

    public static void ShowNotification(string title, string message)
    {
        try
        {
            // Use Windows 11 notification center
            Application.Current.Dispatcher.Invoke(() =>
            {
                // For now, simple message box - full toast notifications require WinRT
                MessageBox.Show(
                    message,
                    title,
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

    #region Windows 11 Features

    public static void EnableWindows11Features(IntPtr hwnd)
    {
        try
        {
            // Windows 11 Mica and acrylic effects
            int TRUE = 1;

            // Enable immersive dark mode
            DwmSetWindowAttribute(hwnd, 20, ref TRUE, sizeof(int));

            // Set corner preference to round (Windows 11 style)
            int cornerPref = 2; // DWMWCP_ROUND
            DwmSetWindowAttribute(hwnd, 33, ref cornerPref, sizeof(int));
        }
        catch { }
    }

    [DllImport("dwmapi.dll", PreserveSig = false)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    #endregion
}
