using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Microsoft.Extensions.DependencyInjection;
using TeachAssistApp.Helpers;

namespace TeachAssistApp.Views;

public partial class DashboardView : Page
{
    private static readonly CubicEase EaseOut = new() { EasingMode = EasingMode.EaseOut };
    private static readonly QuadraticEase SmoothEase = new() { EasingMode = EasingMode.EaseInOut };
    private bool _firstLoad = true;
    private ViewModels.DashboardViewModel? _vm;

    public DashboardView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is ViewModels.DashboardViewModel oldVm)
            oldVm.PropertyChanged -= OnIsLoadingChanged;

        if (e.NewValue is ViewModels.DashboardViewModel newVm)
        {
            _vm = newVm;
            newVm.PropertyChanged += OnIsLoadingChanged;
        }
    }

    private void OnIsLoadingChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModels.DashboardViewModel.IsLoading) && _vm != null)
            Dispatcher.Invoke(() => AnimateLoadingOverlay(_vm.IsLoading));
    }

    private void AnimateLoadingOverlay(bool isLoading)
    {
        // Find the loading overlay grid (first child with OverlayBrush background in row span 2)
        var overlay = FindLoadingOverlay();
        if (overlay == null) return;

        if (isLoading)
        {
            overlay.Visibility = Visibility.Visible;
            overlay.Opacity = 0;
            var fadeIn = new DoubleAnimation
            {
                From = 0, To = 1,
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = EaseOut
            };
            overlay.BeginAnimation(UIElement.OpacityProperty, fadeIn);
        }
        else
        {
            var fadeOut = new DoubleAnimation
            {
                From = 1, To = 0,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = EaseOut
            };
            fadeOut.Completed += (s, _) =>
            {
                overlay.Visibility = Visibility.Collapsed;
                overlay.Opacity = 1;
            };
            overlay.BeginAnimation(UIElement.OpacityProperty, fadeOut);
        }
    }

    private Grid? FindLoadingOverlay()
    {
        // Loading overlay is the grid with Grid.RowSpan="2" and OverlayBrush
        foreach (var child in LogicalTreeHelper.GetChildren(this))
        {
            if (child is Grid g && g.GetValue(Grid.RowSpanProperty) is int rs && rs == 2)
                return g;
        }
        return null;
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
                BentoGrid.Opacity = 1;
                BentoGrid.RenderTransform = new TranslateTransform(0, 0);
                CourseSectionLabel.Opacity = 1;
            }
        }
        catch
        {
            BentoGrid.Opacity = 1;
            CourseSectionLabel.Opacity = 1;
        }
    }

    private void AnimateHeroEntrance()
    {
        if (BentoGrid != null)
        {
            var fadeIn = new DoubleAnimation
            {
                From = 0, To = 1,
                Duration = TimeSpan.FromMilliseconds(600),
                EasingFunction = EaseOut
            };
            var slideUp = new DoubleAnimation
            {
                From = 20, To = 0,
                Duration = TimeSpan.FromMilliseconds(600),
                EasingFunction = EaseOut
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

        if (CourseSectionLabel != null)
        {
            var labelFade = new DoubleAnimation
            {
                From = 0, To = 1,
                Duration = TimeSpan.FromMilliseconds(400),
                BeginTime = TimeSpan.FromMilliseconds(500),
                EasingFunction = EaseOut
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

            var delay = TimeSpan.FromMilliseconds(600 + i * 50);

            var fadeIn = new DoubleAnimation
            {
                From = 0, To = 1,
                Duration = TimeSpan.FromMilliseconds(350),
                BeginTime = delay,
                EasingFunction = EaseOut
            };
            var slideUp = new DoubleAnimation
            {
                From = 16, To = 0,
                Duration = TimeSpan.FromMilliseconds(350),
                BeginTime = delay,
                EasingFunction = EaseOut
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
