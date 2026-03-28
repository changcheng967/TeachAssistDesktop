using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using ScottPlot;
using ScottPlot.WPF;
using TeachAssistApp.Models;
using TeachAssistApp.ViewModels;

namespace TeachAssistApp.Views;

public partial class CourseDetailView : Page
{
    private WpfPlot? _plotControl;
    private CourseDetailViewModel? _viewModel;

    public CourseDetailView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is CourseDetailViewModel vm)
        {
            if (_viewModel != null && _viewModel != vm)
            {
                _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
                _viewModel.GradeTimeline.CollectionChanged -= OnGradeTimelineChanged;
            }

            _viewModel = vm;
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            _viewModel.GradeTimeline.CollectionChanged += OnGradeTimelineChanged;

            if (_plotControl != null)
            {
                ChartHost.Content = null;
                _plotControl = null;
            }

            CreatePlotControl();
            RenderChart(_viewModel.GradeTimeline);
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (_viewModel != null)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            _viewModel.GradeTimeline.CollectionChanged -= OnGradeTimelineChanged;
            _viewModel = null;
        }
    }

    private void CreatePlotControl()
    {
        _plotControl = new WpfPlot();
        _plotControl.UserInputProcessor.Disable();
        ChartHost.Content = _plotControl;
    }

    private void OnGradeTimelineChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_viewModel != null)
            RenderChart(_viewModel.GradeTimeline);
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CourseDetailViewModel.GradeTimeline) && sender is CourseDetailViewModel vm)
        {
            RenderChart(vm.GradeTimeline);
        }
    }

    private void RenderChart(ObservableCollection<GradeTimelinePoint> timeline)
    {
        if (_plotControl == null || timeline.Count == 0) return;

        try
        {
            _plotControl.Dispatcher.Invoke(() =>
            {
                var plot = _plotControl.Plot;
                plot.Clear();

                var isDark = IsDarkTheme();
                if (isDark)
                {
                    plot.FigureBackground.Color = Color.FromHex("#0D1117");
                    plot.DataBackground.Color = Color.FromHex("#161B22");
                    plot.Axes.Color(Color.FromHex("#8B949E"));
                    plot.Grid.MajorLineColor = Color.FromHex("#21262D");
                    plot.Grid.MinorLineColor = Color.FromHex("#161B22");
                }
                else
                {
                    plot.FigureBackground.Color = Color.FromHex("#FFFFFF");
                    plot.DataBackground.Color = Color.FromHex("#F5F5F4");
                    plot.Axes.Color(Color.FromHex("#44403C"));
                    plot.Grid.MajorLineColor = Color.FromHex("#E7E5E4");
                    plot.Grid.MinorLineColor = Color.FromHex("#F5F5F4");
                }

                // Build data arrays — X = assignment index, Y = cumulative grade percentage
                double[] xs = timeline.Select(t => (double)t.Index).ToArray();
                double[] ys = timeline.Select(t => t.CumulativeGrade).ToArray();

                var lineColor = isDark ? Color.FromHex("#58A6FF") : Color.FromHex("#2563EB");
                var hiColor = isDark ? Color.FromHex("#238636") : Color.FromHex("#16A34A");
                var textColor = isDark ? Color.FromHex("#8B949E") : Color.FromHex("#57534E");
                var titleColor = isDark ? Color.FromHex("#C9D1D9") : Color.FromHex("#1C1917");

                // Main cumulative grade line
                var scatter = plot.Add.Scatter(xs, ys);
                scatter.Color = lineColor;
                scatter.LineWidth = 2;
                scatter.MarkerSize = 6;
                scatter.MarkerShape = MarkerShape.FilledCircle;
                scatter.LegendText = "Cumulative Grade";

                // High-impact overlay (larger green dots)
                var highImpactXs = new List<double>();
                var highImpactYs = new List<double>();
                foreach (var point in timeline.Where(t => t.IsHighImpact && !t.FirstPoint))
                {
                    highImpactXs.Add(point.Index);
                    highImpactYs.Add(point.CumulativeGrade);
                }

                if (highImpactXs.Count > 0)
                {
                    var hiScatter = plot.Add.Scatter(highImpactXs.ToArray(), highImpactYs.ToArray());
                    hiScatter.Color = hiColor;
                    hiScatter.LineWidth = 0;
                    hiScatter.MarkerSize = 10;
                    hiScatter.MarkerShape = MarkerShape.FilledCircle;
                    hiScatter.LegendText = "High Impact";
                }

                // Axis label styling
                plot.Axes.Bottom.TickLabelStyle.FontSize = 10;
                plot.Axes.Left.TickLabelStyle.FontSize = 10;
                plot.Axes.Bottom.TickLabelStyle.ForeColor = textColor;
                plot.Axes.Left.TickLabelStyle.ForeColor = textColor;

                // Custom X-axis tick labels — assignment names using NumericManual
                var tickGen = new ScottPlot.TickGenerators.NumericManual();
                for (int i = 0; i < timeline.Count; i++)
                {
                    tickGen.AddMajor(i, Truncate(timeline[i].AssignmentName, 12));
                }
                plot.Axes.Bottom.TickGenerator = tickGen;
                plot.Axes.Bottom.TickLabelStyle.Rotation = 45;

                // Y-axis: grade percentage 0–100
                plot.Axes.Left.Min = 0;
                plot.Axes.Left.Max = 100;

                // X-axis limits
                plot.Axes.Bottom.Min = -0.5;
                plot.Axes.Bottom.Max = xs.Length - 0.5;

                // Legend (top-left)
                plot.Legend.Alignment = Alignment.UpperLeft;
                var legendBg = isDark ? Color.FromHex("#161B22").WithAlpha(0.9) : Color.FromHex("#FFFFFF").WithAlpha(0.9);
                var legendFont = isDark ? Color.FromHex("#C9D1D9") : Color.FromHex("#1C1917");
                var legendOutline = isDark ? Color.FromHex("#30363D") : Color.FromHex("#D6D3D1");
                plot.Legend.BackgroundColor = legendBg;
                plot.Legend.FontColor = legendFont;
                plot.Legend.OutlineColor = legendOutline;
                plot.Legend.OutlineWidth = 1;

                // Title
                plot.Title("Cumulative Grade Over Assignments");
                plot.Axes.Title.Label.ForeColor = titleColor;
                plot.Axes.Bottom.Label.Text = "Assignments";
                plot.Axes.Bottom.Label.ForeColor = textColor;
                plot.Axes.Left.Label.Text = "Grade (%)";
                plot.Axes.Left.Label.ForeColor = textColor;

                plot.ShowLegend();
                _plotControl.Refresh();
            });
        }
        catch (Exception ex)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"Chart render error: {ex.Message}");
#endif
        }
    }

    private static bool IsDarkTheme()
    {
        try
        {
            // Check if the app background brush is dark
            if (Application.Current.Resources["ApplicationPageBackgroundThemeBrush"] is System.Windows.Media.SolidColorBrush brush)
            {
                // Dark theme uses near-black backgrounds
                return brush.Color.R < 128 && brush.Color.G < 128 && brush.Color.B < 128;
            }
        }
        catch { }
        return true; // Default to dark
    }

    private static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength) return value;
        return value[..maxLength] + "\u2026";
    }
}
