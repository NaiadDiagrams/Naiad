namespace MermaidSharp.Rendering;

public class SvgForeignObject : SvgElement
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public required string HtmlContent { get; set; }

    public override string ToXml()
    {
        var builder = new StringBuilder();
        builder.Append($"<foreignObject x=\"{Fmt(X)}\" y=\"{Fmt(Y)}\" width=\"{Fmt(Width)}\" height=\"{Fmt(Height)}\"");
        builder.Append(CommonAttributes());
        builder.Append('>');
        builder.Append($"<div xmlns=\"http://www.w3.org/1999/xhtml\" style=\"display: table-cell; white-space: nowrap; line-height: 1.5; max-width: 200px; text-align: center; vertical-align: middle; width: {Fmt(Width)}px; height: {Fmt(Height)}px;\">");
        builder.Append($"<span class=\"nodeLabel\">{HtmlContent}</span>");
        builder.Append("</div>");
        builder.Append("</foreignObject>");
        return builder.ToString();
    }
}