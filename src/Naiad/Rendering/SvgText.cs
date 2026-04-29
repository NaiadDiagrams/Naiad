namespace Naiad.Rendering;

public class SvgText : SvgElement
{
    public double X { get; init; }
    public double Y { get; init; }
    // When true, don't output x/y attributes (for transformed text)
    public bool OmitXY { get; init; }
    public required string Content { get; init; }
    public string? TextAnchor { get; init; }
    public string? DominantBaseline { get; init; }
    public string? FontSize { get; init; }
    public string? FontFamily { get; init; }
    public string? FontWeight { get; init; }
    public string? Fill { get; init; }

    public override void ToXml(StringBuilder builder)
    {
        builder.Append("<text");

        // For transformed text (OmitXY=true), mermaid.ink uses: transform, class, style order
        if (OmitXY)
        {
            if (Transform is not null)
            {
                builder.Append($" transform=\"{Transform}\"");
            }

            if (Class is not null)
            {
                builder.Append($" class=\"{Class}\"");
            }

            if (Style is not null)
            {
                builder.Append($" style=\"{Style}\"");
            }
        }
        else
        {
            builder.Append(CultureInfo.InvariantCulture, $" x=\"{X:0.##}\" y=\"{Y:0.##}\"");
            if (TextAnchor is not null)
            {
                builder.Append($" text-anchor=\"{TextAnchor}\"");
            }

            if (DominantBaseline is not null)
            {
                builder.Append($" dominant-baseline=\"{DominantBaseline}\"");
            }

            if (FontSize is not null)
            {
                builder.Append($" font-size=\"{FontSize}\"");
            }

            if (FontFamily is not null)
            {
                builder.Append($" font-family=\"{FontFamily}\"");
            }

            if (FontWeight is not null)
            {
                builder.Append($" font-weight=\"{FontWeight}\"");
            }

            if (Fill is not null)
            {
                builder.Append($" fill=\"{Fill}\"");
            }

            CommonAttributes(builder);
        }

        if (string.IsNullOrEmpty(Content))
        {
            builder.Append("/>");
        }
        else
        {
            builder.Append('>');
            AppendEscapedXml(builder, Content);
            builder.Append("</text>");
        }
    }

    static void AppendEscapedXml(StringBuilder builder, string text)
    {
        // Fast path: if no escaping needed, append directly without scanning twice.
        var start = 0;
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            var replacement = c switch
            {
                '&' => "&amp;",
                '<' => "&lt;",
                '>' => "&gt;",
                '"' => "&quot;",
                '\'' => "&apos;",
                _ => null
            };

            if (replacement is null)
            {
                continue;
            }

            if (i > start)
            {
                builder.Append(text, start, i - start);
            }

            builder.Append(replacement);
            start = i + 1;
        }

        if (start == 0)
        {
            builder.Append(text);
        }
        else if (start < text.Length)
        {
            builder.Append(text, start, text.Length - start);
        }
    }
}
