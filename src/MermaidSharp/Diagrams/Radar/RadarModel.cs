using MermaidSharp.Models;

namespace MermaidSharp.Diagrams.Radar;

public class RadarModel : DiagramBase
{
    public List<RadarAxis> Axes { get; } = [];
    public List<RadarCurve> Curves { get; } = [];
    public double? Min { get; set; }
    public double? Max { get; set; }
    public bool ShowLegend { get; set; } = true;
    public GraticuleType Graticule { get; set; } = GraticuleType.Circle;
    public int Ticks { get; set; } = 5;
}

public class RadarAxis
{
    public required string Id { get; init; }
    public string? Label { get; set; }
}

public class RadarCurve
{
    public required string Id { get; init; }
    public string? Label { get; set; }
    public List<double> Values { get; } = [];
    public Dictionary<string, double> NamedValues { get; } = [];
}

public enum GraticuleType
{
    Circle,
    Polygon
}
