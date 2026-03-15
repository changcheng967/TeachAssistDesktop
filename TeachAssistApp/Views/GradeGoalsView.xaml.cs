using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Microsoft.Extensions.DependencyInjection;
using TeachAssistApp.ViewModels;

namespace TeachAssistApp.Views;

public partial class GradeGoalsView : Window
{
    public GradeGoalsView(IServiceProvider serviceProvider)
    {
        InitializeComponent();
        var viewModel = serviceProvider.GetRequiredService<GradeGoalsViewModel>();
        viewModel.RequestClose += (s, e) => Close();
        DataContext = viewModel;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        var border = (Border)Content;
        border.RenderTransform = new ScaleTransform(0.95, 0.95, ActualWidth / 2, ActualHeight / 2);
        border.Opacity = 0;

        var fadeIn = new DoubleAnimation { From = 0, To = 1, Duration = TimeSpan.FromMilliseconds(250), EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
        var scaleX = new DoubleAnimation { From = 0.95, To = 1.0, Duration = TimeSpan.FromMilliseconds(250), EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
        var scaleY = new DoubleAnimation { From = 0.95, To = 1.0, Duration = TimeSpan.FromMilliseconds(250), EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };

        Storyboard.SetTarget(fadeIn, border);
        Storyboard.SetTargetProperty(fadeIn, new PropertyPath(UIElement.OpacityProperty));
        Storyboard.SetTarget(scaleX, border);
        Storyboard.SetTargetProperty(scaleX, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleX)"));
        Storyboard.SetTarget(scaleY, border);
        Storyboard.SetTargetProperty(scaleY, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)"));

        var sb = new Storyboard();
        sb.Children.Add(fadeIn);
        sb.Children.Add(scaleX);
        sb.Children.Add(scaleY);
        sb.Begin(this);
    }
}
