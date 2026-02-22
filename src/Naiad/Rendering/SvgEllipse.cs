namespace MermaidSharp.Rendering;

public class SvgEllipse : SvgElement
{
    public double Cx { get; set; }
    public double Cy { get; set; }
    public double Rx { get; set; }
    public double Ry { get; set; }
    public string? Fill { get; set; }
    public string? Stroke { get; set; }

    public override string ToXml()
    {
        var builder = new StringBuilder($"<ellipse cx=\"{Fmt(Cx)}\" cy=\"{Fmt(Cy)}\" rx=\"{Fmt(Rx)}\" ry=\"{Fmt(Ry)}\"");

        if (Fill is not null)
        {
            builder.Append($" fill=\"{Fill}\"");
        }

        if (Stroke is not null)
        {
            builder.Append($" stroke=\"{Stroke}\"");
        }

        builder.Append(CommonAttributes());
        builder.Append("/>");
        return builder.ToString();
    }
}