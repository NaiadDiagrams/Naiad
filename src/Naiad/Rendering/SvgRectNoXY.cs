namespace MermaidSharp.Rendering;

public class SvgRectNoXY : SvgElement
{
    public double Width { get; set; }
    public double Height { get; set; }

    public override string ToXml()
    {
        var builder = new StringBuilder($"<rect width=\"{Fmt(Width)}\" height=\"{Fmt(Height)}\"");
        builder.Append(CommonAttributes());
        builder.Append("/>");
        return builder.ToString();
    }
}