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