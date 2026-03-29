using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Microsoft.Extensions.DependencyInjection;
using TeachAssistApp.Helpers;

namespace TeachAssistApp.Views;

public partial class DashboardView : Page
{
    private static readonly QuadraticEase SmoothEase = new() { EasingMode = EasingMode.EaseInOut };
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
                BentoGrid.Opacity = 1;
                BentoGrid.RenderTransform = new TranslateTransform(0, 0);
                CourseSectionLabel.Opacity = 1;
            }
        }
        catch
        {
            // Entrance animation is non-critical — ensure visibility
            BentoGrid.Opacity = 1;
            CourseSectionLabel.Opacity = 1;
        }
    }

    private void AnimateHeroEntrance()
    {
        // Bento grid: fade + slide up
        if (BentoGrid != null)
        {
            var fadeIn = new DoubleAnimation
            {
                From = 0, To = 1,
                Duration = TimeSpan.FromMilliseconds(700),
                EasingFunction = SmoothEase
            };
            var slideUp = new DoubleAnimation
            {
                From = 16, To = 0,
                Duration = TimeSpan.FromMilliseconds(700),
                EasingFunction = SmoothEase
            };

            Storyboard.SetTarget(fadeIn, BentoGrid);
            Storyboard.SetTargetProperty(fadeIn, new PropertyPath(UIElement.OpacityProperty));
            Storyboard.SetTarget(slideUp, BentoGrid);
            Storyboard.SetTargetProperty(slideUp, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));

            var sb = new Storyboard();
            sb.Children.Add(fadeIn);
            sb.Children.Add(slideUp);
            sb.Begin(this);
        }

        // "Courses" label: fade in
        if (CourseSectionLabel != null)
        {
            var labelFade = new DoubleAnimation
            {
                From = 0, To = 1,
                Duration = TimeSpan.FromMilliseconds(500),
                BeginTime = TimeSpan.FromMilliseconds(600),
                EasingFunction = SmoothEase
            };
            Storyboard.SetTarget(labelFade, CourseSectionLabel);
            Storyboard.SetTargetProperty(labelFade, new PropertyPath(UIElement.OpacityProperty));

            var sb2 = new Storyboard();
            sb2.Children.Add(labelFade);
            sb2.Begin(this);
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

            var delay = TimeSpan.FromMilliseconds(700 + i * 80);

            var fadeIn = new DoubleAnimation
            {
                From = 0, To = 1,
                Duration = TimeSpan.FromMilliseconds(500),
                BeginTime = delay,
                EasingFunction = SmoothEase
            };
            var slideUp = new DoubleAnimation
            {
                From = 12, To = 0,
                Duration = TimeSpan.FromMilliseconds(500),
                BeginTime = delay,
                EasingFunction = SmoothEase
            };

            container.RenderTransform = new TranslateTransform();
            Storyboard.SetTarget(fadeIn, container);
            Storyboard.SetTargetProperty(fadeIn, new PropertyPath(UIElement.OpacityProperty));
            Storyboard.SetTarget(slideUp, container);
            Storyboard.SetTargetProperty(slideUp, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));

            var sb = new Storyboard();
            sb.Children.Add(fadeIn);
            sb.Children.Add(slideUp);
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
