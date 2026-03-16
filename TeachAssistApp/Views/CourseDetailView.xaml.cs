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

                // Dark theme colors
                plot.FigureBackground.Color = Color.FromHex("#0D1117");
                plot.DataBackground.Color = Color.FromHex("#161B22");
                plot.Axes.Color(Color.FromHex("#8B949E"));
                plot.Grid.MajorLineColor = Color.FromHex("#21262D");
                plot.Grid.MinorLineColor = Color.FromHex("#161B22");

                // Build data arrays — X = assignment index, Y = cumulative grade percentage
                double[] xs = timeline.Select(t => (double)t.Index).ToArray();
                double[] ys = timeline.Select(t => t.CumulativeGrade).ToArray();

                // Main cumulative grade line
                var scatter = plot.Add.Scatter(xs, ys);
                scatter.Color = Color.FromHex("#58A6FF");
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
                    hiScatter.Color = Color.FromHex("#238636");
                    hiScatter.LineWidth = 0;
                    hiScatter.MarkerSize = 10;
                    hiScatter.MarkerShape = MarkerShape.FilledCircle;
                    hiScatter.LegendText = "High Impact";
                }

                // Axis label styling
                plot.Axes.Bottom.TickLabelStyle.FontSize = 10;
                plot.Axes.Left.TickLabelStyle.FontSize = 10;
                plot.Axes.Bottom.TickLabelStyle.ForeColor = Color.FromHex("#8B949E");
                plot.Axes.Left.TickLabelStyle.ForeColor = Color.FromHex("#8B949E");

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

                // Legend (top-left, semi-transparent dark)
                plot.Legend.Alignment = Alignment.UpperLeft;
                plot.Legend.BackgroundColor = Color.FromHex("#161B22").WithAlpha(0.9);
                plot.Legend.FontColor = Color.FromHex("#C9D1D9");
                plot.Legend.OutlineColor = Color.FromHex("#30363D");
                plot.Legend.OutlineWidth = 1;

                // Title
                plot.Title("Cumulative Grade Over Assignments");
                plot.Axes.Title.Label.ForeColor = Color.FromHex("#C9D1D9");
                plot.Axes.Bottom.Label.Text = "Assignments";
                plot.Axes.Bottom.Label.ForeColor = Color.FromHex("#8B949E");
                plot.Axes.Left.Label.Text = "Grade (%)";
                plot.Axes.Left.Label.ForeColor = Color.FromHex("#8B949E");

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

    private static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength) return value;
        return value[..maxLength] + "\u2026";
    }
}
