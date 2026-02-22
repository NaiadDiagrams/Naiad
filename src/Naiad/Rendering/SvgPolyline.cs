namespace MermaidSharp.Rendering;

public class SvgPolyline : SvgElement
{
    public List<Position> Points { get; } = [];
    public string? Fill { get; set; }
    public string? Stroke { get; set; }
    public double? StrokeWidth { get; set; }
    public string? StrokeDasharray { get; set; }
    public string? MarkerEnd { get; set; }

    public override void ToXml(StringBuilder builder)
    {
        builder.Append("<polyline points=\"");
        for (var i = 0; i < Points.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(' ');
            }

            builder.Append(Fmt(Points[i].X));
            builder.Append(',');
            builder.Append(Fmt(Points[i].Y));
        }

        builder.Append('"');

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

        CommonAttributes(builder);
        builder.Append("/>");
    }
}