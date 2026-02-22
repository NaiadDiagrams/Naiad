namespace MermaidSharp.Diagrams.XYChart;

public class ChartSeries
{
    public ChartSeriesType Type { get; init; }
    public List<double> Data { get; init; } = [];
}