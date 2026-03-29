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
    private bool _firstLoad = true;

    public DashboardView()
    {
        InitializeComponent();
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_firstLoad)
            {
                AnimateHeroEntrance();
                AnimateCardsEntrance();
                _firstLoad = false;
            }
            else
            {
                // Ensure hero is visible on subsequent navigations
                HeroStatCard.Opacity = 1;
                HeroStatCard.RenderTransform = new TranslateTransform(0, 0);
                HeroProgressBar.Opacity = 1;
                CourseSectionLabel.Opacity = 1;
            }
        }
        catch
        {
            // Entrance animation is non-critical — ensure visibility
            HeroStatCard.Opacity = 1;
            HeroProgressBar.Opacity = 1;
            CourseSectionLabel.Opacity = 1;
        }
    }

    private void AnimateHeroEntrance()
    {
        // Hero stat card: fade + slide up
        if (HeroStatCard != null)
        {
            var fadeIn = new DoubleAnimation
            {
                From = 0, To = 1,
                Duration = TimeSpan.FromMilliseconds(400),
                EasingFunction = EaseOut
            };
            var slideUp = new DoubleAnimation
            {
                From = 20, To = 0,
                Duration = TimeSpan.FromMilliseconds(400),
                EasingFunction = EaseOut
            };

            Storyboard.SetTarget(fadeIn, HeroStatCard);
            Storyboard.SetTargetProperty(fadeIn, new PropertyPath(UIElement.OpacityProperty));
            Storyboard.SetTarget(slideUp, HeroStatCard);
            Storyboard.SetTargetProperty(slideUp, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));

            var sb = new Storyboard();
            sb.Children.Add(fadeIn);
            sb.Children.Add(slideUp);
            sb.Begin(this);
        }

        // Progress bar: fade in after hero
        if (HeroProgressBar != null)
        {
            var progressFade = new DoubleAnimation
            {
                From = 0, To = 1,
                Duration = TimeSpan.FromMilliseconds(300),
                BeginTime = TimeSpan.FromMilliseconds(200),
                EasingFunction = EaseOut
            };
            Storyboard.SetTarget(progressFade, HeroProgressBar);
            Storyboard.SetTargetProperty(progressFade, new PropertyPath(UIElement.OpacityProperty));

            var sb2 = new Storyboard();
            sb2.Children.Add(progressFade);
            sb2.Begin(this);
        }

        // "Courses" label: fade in
        if (CourseSectionLabel != null)
        {
            var labelFade = new DoubleAnimation
            {
                From = 0, To = 1,
                Duration = TimeSpan.FromMilliseconds(300),
                BeginTime = TimeSpan.FromMilliseconds(350),
                EasingFunction = EaseOut
            };
            Storyboard.SetTarget(labelFade, CourseSectionLabel);
            Storyboard.SetTargetProperty(labelFade, new PropertyPath(UIElement.OpacityProperty));

            var sb3 = new Storyboard();
            sb3.Children.Add(labelFade);
            sb3.Begin(this);
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
