using System.Collections.ObjectModel;
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
            // Clean up previous subscription if any
            if (_viewModel != null && _viewModel != vm)
            {
                _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            }

            _viewModel = vm;
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;

            // Dispose old plot control if re-navigating
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
            _viewModel = null;
        }
    }

    private void CreatePlotControl()
    {
        _plotControl = new WpfPlot();
        _plotControl.UserInputProcessor.Disable();
        ChartHost.Content = _plotControl;
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

        _plotControl.Dispatcher.Invoke(() =>
        {
            var plot = _plotControl.Plot;
            plot.Clear();

            // Dark theme
            plot.FigureBackground.Color = ScottPlot.Color.FromHex("#0D1117");
            plot.DataBackground.Color = ScottPlot.Color.FromHex("#161B22");
            plot.Axes.Color(ScottPlot.Color.FromHex("#8B949E"));
            plot.Grid.MajorLineColor = ScottPlot.Color.FromHex("#21262D");
            plot.Grid.MinorLineColor = ScottPlot.Color.FromHex("#161B22");

            // Data
            double[] xs = timeline.Select(t => (double)t.Index).ToArray();
            double[] ys = timeline.Select(t => t.CumulativeGrade).ToArray();

            // Main line
            var scatter = plot.Add.Scatter(xs, ys);
            scatter.Color = ScottPlot.Color.FromHex("#58A6FF");
            scatter.LineWidth = 2;
            scatter.MarkerSize = 6;
            scatter.MarkerShape = MarkerShape.FilledCircle;
            scatter.LegendText = "Cumulative Grade";

            // High-impact highlights
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
                hiScatter.Color = ScottPlot.Color.FromHex("#238636");
                hiScatter.LineWidth = 0;
                hiScatter.MarkerSize = 10;
                hiScatter.MarkerShape = MarkerShape.FilledCircle;
                hiScatter.LegendText = "High Impact";
            }

            // Axes labels
            plot.Axes.Bottom.TickLabelStyle.FontSize = 10;
            plot.Axes.Left.TickLabelStyle.FontSize = 10;
            plot.Axes.Bottom.TickLabelStyle.ForeColor = ScottPlot.Color.FromHex("#8B949E");
            plot.Axes.Left.TickLabelStyle.ForeColor = ScottPlot.Color.FromHex("#8B949E");

            // Custom tick labels for X axis — show assignment names
            var tickPositions = timeline.Select(t => (double)t.Index).ToArray();
            var tickLabels = timeline.Select(t => Truncate(t.AssignmentName, 12)).ToArray();
            plot.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericManual(tickPositions, tickLabels);
            plot.Axes.Bottom.TickLabelStyle.Rotation = 45;

            // Y axis range
            if (ys.Any())
            {
                double minY = ys.Min();
                double maxY = ys.Max();
                double padding = Math.Max((maxY - minY) * 0.2, 5);
                plot.Axes.SetLimitsY(Math.Max(0, minY - padding), Math.Min(100, maxY + padding));
            }

            plot.Axes.SetLimitsX(-0.5, xs.Length - 0.5);

            // Legend
            plot.Legend.Alignment = Alignment.UpperLeft;
            plot.Legend.BackgroundColor = ScottPlot.Color.FromHex("#161B22").WithAlpha(0.9);
            plot.Legend.FontColor = ScottPlot.Color.FromHex("#C9D1D9");
            plot.Legend.OutlineColor = ScottPlot.Color.FromHex("#30363D");
            plot.Legend.OutlineWidth = 1;

            // Title
            plot.Title("Cumulative Grade Over Assignments");
            plot.Axes.Title.Label.ForeColor = ScottPlot.Color.FromHex("#C9D1D9");

            plot.Axes.Bottom.Label.Text = "Assignments";
            plot.Axes.Bottom.Label.ForeColor = ScottPlot.Color.FromHex("#8B949E");
            plot.Axes.Left.Label.Text = "Grade (%)";
            plot.Axes.Left.Label.ForeColor = ScottPlot.Color.FromHex("#8B949E");

            plot.ShowLegend();
            _plotControl.Refresh();
        });
    }

    private static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength) return value;
        return value[..maxLength] + "\u2026";
    }
}
