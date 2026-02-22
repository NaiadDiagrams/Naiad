namespace MermaidSharp.Rendering;

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

    public override string ToXml()
    {
        var builder = new StringBuilder($"<path d=\"{D}\"");

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
            builder.Append($" opacity=\"{Fmt(Opacity.Value)}\"");
        }

        builder.Append(CommonAttributes());
        builder.Append("/>");
        return builder.ToString();
    }
}