using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace TeachAssistApp.Helpers;

public static class NumericCounterBehavior
{
    public static readonly DependencyProperty TargetValueProperty =
        DependencyProperty.RegisterAttached("TargetValue", typeof(double), typeof(NumericCounterBehavior),
            new PropertyMetadata(0.0, OnTargetValueChanged));

    public static readonly DependencyProperty AnimatedProperty =
        DependencyProperty.RegisterAttached("Animated", typeof(bool), typeof(NumericCounterBehavior),
            new PropertyMetadata(false));

    public static double GetTargetValue(DependencyObject obj) => (double)obj.GetValue(TargetValueProperty);
    public static void SetTargetValue(DependencyObject obj, double value) => obj.SetValue(TargetValueProperty, value);

    public static bool GetAnimated(DependencyObject obj) => (bool)obj.GetValue(AnimatedProperty);
    public static void SetAnimated(DependencyObject obj, bool value) => obj.SetValue(AnimatedProperty, value);

    private static void OnTargetValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBlock tb || !GetAnimated(tb))
            return;

        var newValue = (double)e.NewValue;
        AnimateCounter(tb, newValue);
    }

    public static void AnimateCounter(TextBlock target, double toValue)
    {
        var duration = TimeSpan.FromMilliseconds(600);
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };

        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        var startTime = DateTime.UtcNow;
        var totalMs = duration.TotalMilliseconds;
        var currentText = target.Text;

        // Try to get starting value from current text
        double fromValue = 0;
        if (!string.IsNullOrEmpty(currentText) && double.TryParse(currentText, out var parsed))
            fromValue = parsed;

        timer.Tick += (s, _) =>
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            var progress = Math.Min(elapsed / totalMs, 1.0);
            // Cubic ease-out: 1 - (1-t)^3
            var eased = 1 - Math.Pow(1 - progress, 3);
            var current = fromValue + (toValue - fromValue) * eased;
            target.Text = Math.Round(current, 1).ToString("F1");

            if (progress >= 1.0)
            {
                timer.Stop();
                target.Text = Math.Round(toValue, 1).ToString("F1");
            }
        };
        timer.Start();
    }
}
