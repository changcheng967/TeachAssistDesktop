using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace TeachAssistApp.Helpers;

public class AnimatedFrame : ContentControl
{
    private static readonly CubicEase EaseOut = new() { EasingMode = EasingMode.EaseOut };
    private static readonly CubicEase EaseIn = new() { EasingMode = EasingMode.EaseIn };

    static AnimatedFrame()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(AnimatedFrame),
            new FrameworkPropertyMetadata(typeof(AnimatedFrame)));
    }

    protected override void OnContentChanged(object oldContent, object newContent)
    {
        base.OnContentChanged(oldContent, newContent);

        if (oldContent is not FrameworkElement oldEl || newContent is not FrameworkElement newEl)
            return;

        // Prep new content: start invisible and offset
        newEl.Opacity = 0;
        newEl.RenderTransform = new TranslateTransform(12, 0);

        var fadeOut = new DoubleAnimation
        {
            From = 1, To = 0,
            Duration = TimeSpan.FromMilliseconds(150),
            EasingFunction = EaseIn
        };
        var slideOut = new DoubleAnimation
        {
            From = 0, To = -12,
            Duration = TimeSpan.FromMilliseconds(150),
            EasingFunction = EaseIn
        };

        var fadeIn = new DoubleAnimation
        {
            From = 0, To = 1,
            Duration = TimeSpan.FromMilliseconds(250),
            EasingFunction = EaseOut,
            BeginTime = TimeSpan.FromMilliseconds(100)
        };
        var slideIn = new DoubleAnimation
        {
            From = 12, To = 0,
            Duration = TimeSpan.FromMilliseconds(250),
            EasingFunction = EaseOut,
            BeginTime = TimeSpan.FromMilliseconds(100)
        };

        Storyboard.SetTarget(fadeOut, oldEl);
        Storyboard.SetTargetProperty(fadeOut, new PropertyPath(OpacityProperty));
        Storyboard.SetTarget(slideOut, oldEl);
        Storyboard.SetTargetProperty(slideOut, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));

        Storyboard.SetTarget(fadeIn, newEl);
        Storyboard.SetTargetProperty(fadeIn, new PropertyPath(OpacityProperty));
        Storyboard.SetTarget(slideIn, newEl);
        Storyboard.SetTargetProperty(slideIn, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));

        var sb = new Storyboard();
        sb.Children.Add(fadeOut);
        sb.Children.Add(slideOut);
        sb.Children.Add(fadeIn);
        sb.Children.Add(slideIn);
        sb.Completed += (_, _) =>
        {
            oldEl.Opacity = 1;
            oldEl.RenderTransform = null;
            newEl.RenderTransform = null;
        };
        sb.Begin(this);
    }
}
