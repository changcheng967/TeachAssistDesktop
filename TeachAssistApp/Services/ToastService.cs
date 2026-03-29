using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace TeachAssistApp.Services;

public interface IToastService
{
    void ShowSuccess(string message, int durationMs = 3000);
    void ShowError(string message, int durationMs = 4000);
    void ShowInfo(string message, int durationMs = 3000);
}

public class ToastService : IToastService
{
    private Panel? _host;
    private readonly DispatcherTimer _clearTimer = new() { Interval = TimeSpan.FromSeconds(5) };

    public void SetHost(Panel host)
    {
        _host = host;
        _clearTimer.Tick += (_, _) =>
        {
            if (_host != null)
            {
                foreach (var child in _host.Children.OfType<Border>().ToList())
                    AnimateOut(child);
            }
        };
    }

    public void ShowSuccess(string message, int durationMs = 3000)
        => ShowToast(message, "#16A34A", durationMs);

    public void ShowError(string message, int durationMs = 4000)
        => ShowToast(message, "#DC2626", durationMs);

    public void ShowInfo(string message, int durationMs = 3000)
        => ShowToast(message, "#2563EB", durationMs);

    private void ShowToast(string message, string accentColor, int durationMs)
    {
        if (_host == null) return;

        _host.Dispatcher.Invoke(() =>
        {
            var accent = (Color)ColorConverter.ConvertFromString(accentColor);

            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(230, 40, 40, 40)),
                BorderBrush = new SolidColorBrush(accent),
                BorderThickness = new Thickness(1, 0, 0, 0),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(16, 10, 16, 10),
                Margin = new Thickness(0, 0, 0, 8),
                MaxWidth = 360,
                HorizontalAlignment = HorizontalAlignment.Right,
                Opacity = 0,
                RenderTransform = new TranslateTransform(20, 0),
                RenderTransformOrigin = new Point(1, 0.5)
            };

            var text = new TextBlock
            {
                Text = message,
                Foreground = new SolidColorBrush(Colors.White),
                FontSize = 13,
                TextWrapping = TextWrapping.Wrap
            };
            border.Child = text;

            _host.Children.Insert(0, border);

            // Fade + slide in
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(250))
            { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
            var slideIn = new DoubleAnimation(20, 0, TimeSpan.FromMilliseconds(250))
            { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
            border.BeginAnimation(UIElement.OpacityProperty, fadeIn);
            border.RenderTransform.BeginAnimation(TranslateTransform.XProperty, slideIn);

            // Auto-remove
            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(durationMs) };
            timer.Tick += (_, _) =>
            {
                timer.Stop();
                AnimateOut(border);
            };
            timer.Start();
        });
    }

    private void AnimateOut(Border border)
    {
        var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(200));
        fadeOut.Completed += (_, _) =>
        {
            _host?.Children.Remove(border);
        };
        border.BeginAnimation(UIElement.OpacityProperty, fadeOut);
    }
}
