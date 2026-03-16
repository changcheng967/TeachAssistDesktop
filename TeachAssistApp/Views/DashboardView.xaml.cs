using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Microsoft.Extensions.DependencyInjection;
using TeachAssistApp.Helpers;

namespace TeachAssistApp.Views;

public partial class DashboardView : Page
{
    public DashboardView()
    {
        InitializeComponent();
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            AnimateCardsEntrance();
        }
        catch
        {
            // Entrance animation is non-critical, swallow errors
        }
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

    private void AnimateCardsEntrance()
    {
        if (CourseItemsControl.Items.Count == 0)
        {
            CourseItemsControl.ItemContainerGenerator.StatusChanged += (s, args) =>
            {
                try
                {
                    if (CourseItemsControl.ItemContainerGenerator.Status == System.Windows.Controls.Primitives.GeneratorStatus.ContainersGenerated
                        && CourseItemsControl.Items.Count > 0)
                    {
                        AnimateCardsEntrance();
                    }
                }
                catch { }
            };
            return;
        }

        for (int i = 0; i < CourseItemsControl.Items.Count; i++)
        {
            var container = CourseItemsControl.ItemContainerGenerator.ContainerFromIndex(i) as ContentPresenter;
            if (container == null) continue;

            // Only animate opacity to avoid conflicting with hover RenderTransform
            var delay = TimeSpan.FromMilliseconds(i * 50);

            var fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(300),
                BeginTime = delay,
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            Storyboard.SetTarget(fadeIn, container);
            Storyboard.SetTargetProperty(fadeIn, new PropertyPath(UIElement.OpacityProperty));

            var sb = new Storyboard();
            sb.Children.Add(fadeIn);
            sb.Begin(this);
        }
    }
}
