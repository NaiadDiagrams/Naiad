namespace Naiad.Rendering;

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

            var point = Points[i];
            builder.Append(CultureInfo.InvariantCulture, $"{point.X:0.##},{point.Y:0.##}");
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
            builder.Append(CultureInfo.InvariantCulture, $" stroke-width=\"{StrokeWidth.Value:0.##}\"");
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
