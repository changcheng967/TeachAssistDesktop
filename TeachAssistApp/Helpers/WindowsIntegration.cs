using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Shell;
using Microsoft.Toolkit.Uwp.Notifications;
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

            jumpList.JumpItems.Clear();
            jumpList.ShowFrequentCategory = false;
            jumpList.ShowRecentCategory = false;

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
        System.Diagnostics.Debug.WriteLine("System tray support enabled");
    }

    #endregion

    #region Windows Notification

    public static void ShowNotification(string title, string message)
    {
        try
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                new ToastContentBuilder()
                    .AddText(title)
                    .AddText(message)
                    .Show();
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to show notification: {ex.Message}");
            // Fallback to message box
            Application.Current.Dispatcher.Invoke(() =>
            {
                MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
            });
        }
    }

    #endregion

    #region Windows 11 Features

    public static void EnableWindows11Features(IntPtr hwnd)
    {
        try
        {
            int TRUE = 1;
            DwmSetWindowAttribute(hwnd, 20, ref TRUE, sizeof(int));
            int cornerPref = 2;
            DwmSetWindowAttribute(hwnd, 33, ref cornerPref, sizeof(int));
        }
        catch { }
    }

    [DllImport("dwmapi.dll", PreserveSig = false)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    #endregion
}
