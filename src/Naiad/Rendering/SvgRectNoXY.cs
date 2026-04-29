namespace MermaidSharp.Rendering;

public class SvgRectNoXY : SvgElement
{
    public double Width { get; set; }
    public double Height { get; set; }

    public override void ToXml(StringBuilder builder)
    {
        builder.Append(CultureInfo.InvariantCulture, $"<rect width=\"{Width:0.##}\" height=\"{Height:0.##}\"");
        CommonAttributes(builder);
        builder.Append("/>");
    }
}
