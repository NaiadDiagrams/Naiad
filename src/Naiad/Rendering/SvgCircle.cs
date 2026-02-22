namespace MermaidSharp.Rendering;

public class SvgCircle : SvgElement
{
    public double Cx { get; set; }
    public double Cy { get; set; }
    public double R { get; set; }
    public string? Fill { get; set; }
    public string? Stroke { get; set; }
    public double? StrokeWidth { get; set; }

    public override void ToXml(StringBuilder builder)
    {
        builder.Append($"<circle cx=\"{Fmt(Cx)}\" cy=\"{Fmt(Cy)}\" r=\"{Fmt(R)}\"");

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