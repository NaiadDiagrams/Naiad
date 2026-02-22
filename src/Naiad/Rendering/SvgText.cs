namespace MermaidSharp.Rendering;

public class SvgText : SvgElement
{
    public double X { get; set; }
    public double Y { get; set; }
    public bool OmitXY { get; set; } // When true, don't output x/y attributes (for transformed text)
    public required string Content { get; set; }
    public string? TextAnchor { get; set; }
    public string? DominantBaseline { get; set; }
    public string? FontSize { get; set; }
    public string? FontFamily { get; set; }
    public string? FontWeight { get; set; }
    public string? Fill { get; set; }

    public override string ToXml()
    {
        var builder = new StringBuilder();
        builder.Append("<text");

        // For transformed text (OmitXY=true), mermaid.ink uses: transform, class, style order
        if (OmitXY)
        {
            if (Transform is not null) builder.Append($" transform=\"{Transform}\"");
            if (Class is not null) builder.Append($" class=\"{Class}\"");
            if (Style is not null) builder.Append($" style=\"{Style}\"");
        }
        else
        {
            builder.Append($" x=\"{Fmt(X)}\" y=\"{Fmt(Y)}\"");
            if (TextAnchor is not null) builder.Append($" text-anchor=\"{TextAnchor}\"");
            if (DominantBaseline is not null) builder.Append($" dominant-baseline=\"{DominantBaseline}\"");
            if (FontSize is not null) builder.Append($" font-size=\"{FontSize}\"");
            if (FontFamily is not null) builder.Append($" font-family=\"{FontFamily}\"");
            if (FontWeight is not null) builder.Append($" font-weight=\"{FontWeight}\"");
            if (Fill is not null) builder.Append($" fill=\"{Fill}\"");
            builder.Append(CommonAttributes());
        }

        if (string.IsNullOrEmpty(Content))
        {
            builder.Append("/>");
        }
        else
        {
            builder.Append($">{EscapeXml(Content)}</text>");
        }
        return builder.ToString();
    }

    static string EscapeXml(string text) =>
        text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&apos;");
}