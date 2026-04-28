using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace TeachAssistApp.Helpers;

/// <summary>
/// Attached behavior that provides macOS-style smooth/inertial scrolling on ScrollViewer.
/// Intercepts mouse wheel events and smoothly animates to the target scroll offset.
/// </summary>
public static class SmoothScrollBehavior
{
    #region Attached Property: IsEnabled

    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached("IsEnabled", typeof(bool), typeof(SmoothScrollBehavior),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);
    public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);

    #endregion

    #region Attached Property: SmoothOffset (internal animation driver)

    private static readonly DependencyProperty SmoothOffsetProperty =
        DependencyProperty.RegisterAttached("SmoothOffset", typeof(double), typeof(SmoothScrollBehavior),
            new PropertyMetadata(0.0, OnSmoothOffsetChanged));

    private static void OnSmoothOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ScrollViewer sv)
        {
            sv.ScrollToVerticalOffset((double)e.NewValue);
        }
    }

    #endregion

    // Per-ScrollViewer state
    private static readonly Dictionary<ScrollViewer, double> _targetOffsets = [];
    private static readonly Dictionary<ScrollViewer, int> _accumulatedDelta = [];

    private const double ScrollStep = 52.0;      // pixels per wheel notch
    private const int AnimFrameMs = 16;          // ~60fps
    private const double LerpFactor = 0.18;      // interpolation speed per frame
    private const double SnapThreshold = 0.5;    // stop animating when this close

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ScrollViewer sv) return;

        if ((bool)e.NewValue)
        {
            sv.PreviewMouseWheel += OnPreviewMouseWheel;
            sv.Unloaded += OnUnloaded;
            // Initialize offset
            sv.SetValue(SmoothOffsetProperty, sv.VerticalOffset);
            sv.Loaded += (_, _) => sv.SetValue(SmoothOffsetProperty, sv.VerticalOffset);
        }
        else
        {
            sv.PreviewMouseWheel -= OnPreviewMouseWheel;
            Cleanup(sv);
        }
    }

    private static void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (sender is not ScrollViewer sv) return;

        e.Handled = true;

        var direction = -Math.Sign(e.Delta);
        var delta = direction * ScrollStep;

        // Get current target (or current position if no pending scroll)
        if (!_targetOffsets.TryGetValue(sv, out var currentTarget))
        {
            currentTarget = (double)sv.GetValue(SmoothOffsetProperty);
        }

        var newTarget = currentTarget + delta;
        newTarget = Math.Max(0, Math.Min(newTarget, sv.ScrollableHeight));

        _targetOffsets[sv] = newTarget;

        // Start animation loop if not already running
        if (!_accumulatedDelta.ContainsKey(sv))
        {
            _accumulatedDelta[sv] = 0;
            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(AnimFrameMs) };
            timer.Tick += (_, _) => AnimateFrame(sv, timer);
            timer.Start();
        }
    }

    private static void AnimateFrame(ScrollViewer sv, DispatcherTimer timer)
    {
        if (!_targetOffsets.TryGetValue(sv, out var target))
        {
            StopAnimation(sv, timer);
            return;
        }

        var current = (double)sv.GetValue(SmoothOffsetProperty);
        var diff = target - current;

        if (Math.Abs(diff) < SnapThreshold)
        {
            sv.SetValue(SmoothOffsetProperty, target);
            sv.ScrollToVerticalOffset(target);
            StopAnimation(sv, timer);
            return;
        }

        // Smooth interpolation (exponential ease-out)
        var step = diff * LerpFactor;
        var next = current + step;

        sv.SetValue(SmoothOffsetProperty, next);
    }

    private static void StopAnimation(ScrollViewer sv, DispatcherTimer timer)
    {
        timer.Stop();
        _targetOffsets.Remove(sv);
        _accumulatedDelta.Remove(sv);
    }

    private static void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (sender is ScrollViewer sv)
            Cleanup(sv);
    }

    private static void Cleanup(ScrollViewer sv)
    {
        _targetOffsets.Remove(sv);
        _accumulatedDelta.Remove(sv);
    }
}
