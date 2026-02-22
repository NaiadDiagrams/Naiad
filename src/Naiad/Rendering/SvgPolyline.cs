namespace MermaidSharp.Rendering;

public class SvgPolyline : SvgElement
{
    public List<Position> Points { get; } = [];
    public string? Fill { get; set; }
    public string? Stroke { get; set; }
    public double? StrokeWidth { get; set; }
    public string? StrokeDasharray { get; set; }
    public string? MarkerEnd { get; set; }

    public override string ToXml()
    {
        var pointsStr = string.Join(" ", Points.Select(_ => $"{Fmt(_.X)},{Fmt(_.Y)}"));
        var builder = new StringBuilder($"<polyline points=\"{pointsStr}\"");

        if (Fill is not null)
        {
            builder.Append($" fill=\"{Fill}\"");
        }

        if (Stroke is not null)
        {
            builder.Append($" stroke=\"{Stroke}\"");
        }

        if (StrokeWidth.HasValue)
        {
            builder.Append($" stroke-width=\"{Fmt(StrokeWidth.Value)}\"");
        }

        if (StrokeDasharray is not null)
        {
            builder.Append($" stroke-dasharray=\"{StrokeDasharray}\"");
        }

        if (MarkerEnd is not null)
        {
            builder.Append($" marker-end=\"{MarkerEnd}\"");
        }

        builder.Append(CommonAttributes());
        builder.Append("/>");
        return builder.ToString();
    }
}