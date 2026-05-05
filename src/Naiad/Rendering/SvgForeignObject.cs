namespace Naiad.Rendering;

public class SvgForeignObject : SvgElement
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public required string HtmlContent { get; set; }

    public override void ToXml(StringBuilder builder)
    {
        builder.Append(CultureInfo.InvariantCulture, $"<foreignObject x=\"{X:0.##}\" y=\"{Y:0.##}\" width=\"{Width:0.##}\" height=\"{Height:0.##}\"");
        CommonAttributes(builder);
        builder.Append('>');
        builder.Append(CultureInfo.InvariantCulture, $"<div xmlns=\"http://www.w3.org/1999/xhtml\" style=\"display: table-cell; white-space: nowrap; line-height: 1.5; max-width: 200px; text-align: center; vertical-align: middle; width: {Width:0.##}px; height: {Height:0.##}px;\">");
        builder.Append($"<span class=\"nodeLabel\">{HtmlContent}</span>");
        builder.Append("</div>");
        builder.Append("</foreignObject>");
    }
}
