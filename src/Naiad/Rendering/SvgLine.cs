namespace MermaidSharp.Rendering;

public class SvgLine : SvgElement
{
    public double X1 { get; set; }
    public double Y1 { get; set; }
    public double X2 { get; set; }
    public double Y2 { get; set; }
    public string? Stroke { get; set; }
    public double? StrokeWidth { get; set; }
    public string? StrokeDasharray { get; set; }

    public override void ToXml(StringBuilder builder)
    {
        builder.Append($"<line x1=\"{Fmt(X1)}\" y1=\"{Fmt(Y1)}\" x2=\"{Fmt(X2)}\" y2=\"{Fmt(Y2)}\"");

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

        CommonAttributes(builder);
        builder.Append("/>");
    }
}