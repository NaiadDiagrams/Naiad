namespace MermaidSharp.Rendering;

public class SvgRect : SvgElement
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public double Rx { get; set; }
    public double Ry { get; set; }
    public string? Fill { get; set; }
    public string? Stroke { get; set; }
    public double? StrokeWidth { get; set; }

    public override void ToXml(StringBuilder builder)
    {
        builder.Append($"<rect x=\"{Fmt(X)}\" y=\"{Fmt(Y)}\" width=\"{Fmt(Width)}\" height=\"{Fmt(Height)}\"");

        if (Rx > 0)
        {
            builder.Append($" rx=\"{Fmt(Rx)}\"");
        }

        if (Ry > 0)
        {
            builder.Append($" ry=\"{Fmt(Ry)}\"");
        }

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

        CommonAttributes(builder);
        builder.Append("/>");
    }
}