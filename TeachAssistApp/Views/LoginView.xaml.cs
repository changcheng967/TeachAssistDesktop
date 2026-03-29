using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Wpf.Ui.Controls;

namespace TeachAssistApp.Views;

public partial class LoginView : Page
{
    private bool _firstLoad = true;
    private static readonly CubicEase EaseOut = new() { EasingMode = EasingMode.EaseOut };

    public LoginView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (!_firstLoad)
        {
            // Ensure everything is visible on subsequent loads
            BrandLogo.Opacity = 1;
            BrandLogo.RenderTransform = new ScaleTransform(1, 1);
            BrandAccentLine.Opacity = 1;
            BrandTitle.Opacity = 1;
            BrandTitle.RenderTransform = new TranslateTransform(0, 0);
            BrandSubtitle.Opacity = 1;
            BrandSubtitle.RenderTransform = new TranslateTransform(0, 0);
            BrandBadges.Opacity = 1;
            BrandBadges.RenderTransform = new TranslateTransform(0, 0);
            FormPanel.Opacity = 1;
            FormPanel.RenderTransform = new TranslateTransform(0, 0);
            return;
        }
        _firstLoad = false;

        var ease = EaseOut;

        // 1. Logo: scale from 0.9 + fade in
        if (BrandLogo != null)
        {
            var logoFade = new DoubleAnimation
            {
                From = 0, To = 1,
                Duration = TimeSpan.FromMilliseconds(500),
                EasingFunction = ease
            };
            var logoScaleX = new DoubleAnimation
            {
                From = 0.9, To = 1.0,
                Duration = TimeSpan.FromMilliseconds(500),
                EasingFunction = ease
            };
            var logoScaleY = new DoubleAnimation
            {
                From = 0.9, To = 1.0,
                Duration = TimeSpan.FromMilliseconds(500),
                EasingFunction = ease
            };

            Storyboard.SetTarget(logoFade, BrandLogo);
            Storyboard.SetTargetProperty(logoFade, new PropertyPath(OpacityProperty));
            Storyboard.SetTarget(logoScaleX, BrandLogo);
            Storyboard.SetTargetProperty(logoScaleX, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleX)"));
            Storyboard.SetTarget(logoScaleY, BrandLogo);
            Storyboard.SetTargetProperty(logoScaleY, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)"));

            var logoSb = new Storyboard();
            logoSb.Children.Add(logoFade);
            logoSb.Children.Add(logoScaleX);
            logoSb.Children.Add(logoScaleY);
            logoSb.Begin(this);
        }

        // 2. Accent line: fade + width expand
        if (BrandAccentLine != null)
        {
            var lineFade = new DoubleAnimation
            {
                From = 0, To = 1,
                Duration = TimeSpan.FromMilliseconds(400),
                BeginTime = TimeSpan.FromMilliseconds(150),
                EasingFunction = ease
            };
            Storyboard.SetTarget(lineFade, BrandAccentLine);
            Storyboard.SetTargetProperty(lineFade, new PropertyPath(OpacityProperty));

            var lineSb = new Storyboard();
            lineSb.Children.Add(lineFade);
            lineSb.Begin(this);
        }

        // 3. Title + Subtitle + Badges: staggered fade+slide up
        FrameworkElement[] textElements = [BrandTitle, BrandSubtitle, BrandBadges];
        for (int i = 0; i < textElements.Length; i++)
        {
            var el = textElements[i];
            var delay = TimeSpan.FromMilliseconds(250 + i * 100);

            var fadeIn = new DoubleAnimation
            {
                From = 0, To = 1,
                Duration = TimeSpan.FromMilliseconds(400),
                BeginTime = delay,
                EasingFunction = ease
            };
            var slideUp = new DoubleAnimation
            {
                From = 12, To = 0,
                Duration = TimeSpan.FromMilliseconds(400),
                BeginTime = delay,
                EasingFunction = ease
            };

            Storyboard.SetTarget(fadeIn, el);
            Storyboard.SetTargetProperty(fadeIn, new PropertyPath(OpacityProperty));
            Storyboard.SetTarget(slideUp, el);
            Storyboard.SetTargetProperty(slideUp, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));

            var sb = new Storyboard();
            sb.Children.Add(fadeIn);
            sb.Children.Add(slideUp);
            sb.Begin(this);
        }

        // 4. Form panel: slide from right + fade
        if (FormPanel != null)
        {
            var formFade = new DoubleAnimation
            {
                From = 0, To = 1,
                Duration = TimeSpan.FromMilliseconds(500),
                BeginTime = TimeSpan.FromMilliseconds(400),
                EasingFunction = ease
            };
            var formSlide = new DoubleAnimation
            {
                From = 20, To = 0,
                Duration = TimeSpan.FromMilliseconds(500),
                BeginTime = TimeSpan.FromMilliseconds(400),
                EasingFunction = ease
            };

            Storyboard.SetTarget(formFade, FormPanel);
            Storyboard.SetTargetProperty(formFade, new PropertyPath(OpacityProperty));
            Storyboard.SetTarget(formSlide, FormPanel);
            Storyboard.SetTargetProperty(formSlide, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));

            var formSb = new Storyboard();
            formSb.Children.Add(formFade);
            formSb.Children.Add(formSlide);
            formSb.Begin(this);
        }
    }

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.LoginViewModel viewModel && sender is Wpf.Ui.Controls.PasswordBox passwordBox)
        {
            viewModel.Password = passwordBox.Password;
        }
    }

    private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            if (DataContext is ViewModels.LoginViewModel viewModel)
            {
                viewModel.LoginCommand.Execute(null);
            }
        }
    }
}
