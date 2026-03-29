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
    private static readonly CubicEase EaseOut = new() { EasingMode = EasingMode.EaseOut };

    public DashboardView()
    {
        InitializeComponent();
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            AnimateStatsEntrance();
            AnimateCardsEntrance();
        }
        catch
        {
            // Entrance animation is non-critical
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

    private void AnimateStatsEntrance()
    {
        var cards = new[] { StatAvg, StatGpa, StatCourses };
        for (int i = 0; i < cards.Length; i++)
        {
            var card = cards[i];
            if (card == null) continue;

            var delay = TimeSpan.FromMilliseconds(i * 80);

            var fadeIn = new DoubleAnimation
            {
                From = 0, To = 1,
                Duration = TimeSpan.FromMilliseconds(300),
                BeginTime = delay,
                EasingFunction = EaseOut
            };
            var slideUp = new DoubleAnimation
            {
                From = 20, To = 0,
                Duration = TimeSpan.FromMilliseconds(300),
                BeginTime = delay,
                EasingFunction = EaseOut
            };

            Storyboard.SetTarget(fadeIn, card);
            Storyboard.SetTargetProperty(fadeIn, new PropertyPath(UIElement.OpacityProperty));
            Storyboard.SetTarget(slideUp, card);
            Storyboard.SetTargetProperty(slideUp, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));

            var sb = new Storyboard();
            sb.Children.Add(fadeIn);
            sb.Children.Add(slideUp);
            sb.Begin(this);
        }
    }

    private bool _cardsAnimating;
    private bool _cardsHandlerAttached;
    private void AnimateCardsEntrance()
    {
        if (_cardsAnimating) return;
        if (CourseItemsControl.Items.Count == 0)
        {
            if (_cardsHandlerAttached) return;
            _cardsHandlerAttached = true;
            CourseItemsControl.ItemContainerGenerator.StatusChanged += OnCardsStatusChanged;
            return;
        }

        _cardsAnimating = true;
        _cardsHandlerAttached = false;

        for (int i = 0; i < CourseItemsControl.Items.Count; i++)
        {
            var container = CourseItemsControl.ItemContainerGenerator.ContainerFromIndex(i) as ContentPresenter;
            if (container == null) continue;

            var delay = TimeSpan.FromMilliseconds(i * 50);

            var fadeIn = new DoubleAnimation
            {
                From = 0, To = 1,
                Duration = TimeSpan.FromMilliseconds(300),
                BeginTime = delay,
                EasingFunction = EaseOut
            };

            Storyboard.SetTarget(fadeIn, container);
            Storyboard.SetTargetProperty(fadeIn, new PropertyPath(UIElement.OpacityProperty));

            var sb = new Storyboard();
            sb.Children.Add(fadeIn);
            sb.Begin(this);
        }
    }

    private void OnCardsStatusChanged(object? sender, EventArgs e)
    {
        try
        {
            if (CourseItemsControl.ItemContainerGenerator.Status == System.Windows.Controls.Primitives.GeneratorStatus.ContainersGenerated
                && CourseItemsControl.Items.Count > 0)
            {
                CourseItemsControl.ItemContainerGenerator.StatusChanged -= OnCardsStatusChanged;
                _cardsAnimating = false;
                _cardsHandlerAttached = false;
                AnimateCardsEntrance();
            }
        }
        catch { }
    }
}
