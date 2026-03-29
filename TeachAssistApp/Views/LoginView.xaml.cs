using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using Wpf.Ui.Controls;

namespace TeachAssistApp.Views;

public partial class LoginView : Page
{
    private bool _firstLoad = true;

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
            BrandTitle.Opacity = 1;
            BrandSubtitle.Opacity = 1;
            BrandBadges.Opacity = 1;
            FormPanel.Opacity = 1;
            return;
        }
        _firstLoad = false;

        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
        FrameworkElement[] brandElements = [BrandLogo, BrandTitle, BrandSubtitle, BrandBadges];

        for (int i = 0; i < brandElements.Length; i++)
        {
            var el = brandElements[i];
            var delay = TimeSpan.FromMilliseconds(i * 80);

            var fadeIn = new DoubleAnimation
            {
                From = 0, To = 1,
                Duration = TimeSpan.FromMilliseconds(400),
                BeginTime = delay,
                EasingFunction = ease
            };
            var slideUp = new DoubleAnimation
            {
                From = 16, To = 0,
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

        // Form panel fades in after brand elements
        var formFade = new DoubleAnimation
        {
            From = 0, To = 1,
            Duration = TimeSpan.FromMilliseconds(500),
            BeginTime = TimeSpan.FromMilliseconds(320),
            EasingFunction = ease
        };
        var formSlide = new DoubleAnimation
        {
            From = 20, To = 0,
            Duration = TimeSpan.FromMilliseconds(500),
            BeginTime = TimeSpan.FromMilliseconds(320),
            EasingFunction = ease
        };

        Storyboard.SetTarget(formFade, FormPanel);
        Storyboard.SetTargetProperty(formFade, new PropertyPath(OpacityProperty));
        Storyboard.SetTarget(formSlide, FormPanel);
        Storyboard.SetTargetProperty(formSlide, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));

        var formSb = new Storyboard();
        formSb.Children.Add(formFade);
        formSb.Children.Add(formSlide);
        formSb.Begin(this);
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
