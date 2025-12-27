using MermaidSharp.Core.Models;

namespace MermaidSharp.Diagrams.XYChart;

public class XYChartModel : DiagramBase
{
    public string? XAxisLabel { get; set; }
    public List<string> XAxisCategories { get; } = [];
    public string? YAxisLabel { get; set; }
    public double? YAxisMin { get; set; }
    public double? YAxisMax { get; set; }
    public List<ChartSeries> Series { get; } = [];
}

public class ChartSeries
{
    public ChartSeriesType Type { get; init; }
    public List<double> Data { get; init; } = [];
}

public enum ChartSeriesType
{
    Bar,
    Line
}
