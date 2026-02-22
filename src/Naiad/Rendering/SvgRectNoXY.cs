namespace MermaidSharp.Rendering;

public class SvgRectNoXY : SvgElement
{
    public double Width { get; set; }
    public double Height { get; set; }

    public override void ToXml(StringBuilder builder)
    {
        builder.Append($"<rect width=\"{Fmt(Width)}\" height=\"{Fmt(Height)}\"");
        CommonAttributes(builder);
        builder.Append("/>");
    }
}