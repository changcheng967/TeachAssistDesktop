using System;
using System.Windows;
using System.Windows.Shell;
using Microsoft.Toolkit.Uwp.Notifications;
using TeachAssistApp.Models;

namespace TeachAssistApp.Helpers;

public static class WindowsIntegration
{
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
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"Failed to update Jump List: {ex.Message}");
#endif
        }
    }

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
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"Failed to show notification: {ex.Message}");
#endif
            Application.Current.Dispatcher.Invoke(() =>
            {
                MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
            });
        }
    }
}
