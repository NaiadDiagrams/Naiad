namespace Naiad.Rendering;

public class SvgPath : SvgElement
{
    public required string D { get; set; }
    public string? Fill { get; set; }
    public string? Stroke { get; set; }
    public double? StrokeWidth { get; set; }
    public string? StrokeDasharray { get; set; }
    public string? MarkerStart { get; set; }
    public string? MarkerEnd { get; set; }
    public double? Opacity { get; set; }

    public override void ToXml(StringBuilder builder)
    {
        builder.Append($"<path d=\"{D}\"");

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

        if (MarkerStart is not null)
        {
            builder.Append($" marker-start=\"{MarkerStart}\"");
        }

        if (MarkerEnd is not null)
        {
            builder.Append($" marker-end=\"{MarkerEnd}\"");
        }

        if (Opacity.HasValue)
        {
            builder.Append(CultureInfo.InvariantCulture, $" opacity=\"{Opacity.Value:0.##}\"");
        }

        CommonAttributes(builder);
        builder.Append("/>");
    }
}
