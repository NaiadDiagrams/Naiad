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

    public override void ToXml(StringBuilder builder)
    {
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
            CommonAttributes(builder);
        }

        if (string.IsNullOrEmpty(Content))
        {
            builder.Append("/>");
        }
        else
        {
            builder.Append($">{EscapeXml(Content)}</text>");
        }
    }

    static string EscapeXml(string text)
    {
        // Fast path: check if escaping is needed at all
        var needsEscape = false;
        foreach (var c in text)
        {
            if (c is '&' or '<' or '>' or '"' or '\'')
            {
                needsEscape = true;
                break;
            }
        }

        if (!needsEscape)
        {
            return text;
        }

        // Single-pass escape
        var sb = new StringBuilder(text.Length + 8);
        foreach (var c in text)
        {
            switch (c)
            {
                case '&': sb.Append("&amp;"); break;
                case '<': sb.Append("&lt;"); break;
                case '>': sb.Append("&gt;"); break;
                case '"': sb.Append("&quot;"); break;
                case '\'': sb.Append("&apos;"); break;
                default: sb.Append(c); break;
            }
        }

        return sb.ToString();
    }
}