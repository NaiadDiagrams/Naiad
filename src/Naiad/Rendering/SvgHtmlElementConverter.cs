namespace MermaidSharp.Rendering;

public static class SvgHtmlElementConverter
{
    public static void ConvertForeignObjectsToNativeText(SvgDocument svg, RenderOptions options)
    {
        svg.FontAwesomeImport = null;

        ConvertForeignObjectsToNativeText(svg.Elements, options);
    }

    private static void ConvertForeignObjectsToNativeText(List<SvgElement> elements, RenderOptions options)
    {
        for (var i = 0; i < elements.Count; i++)
        {
            switch (elements[i])
            {
                case SvgForeignObject foreignObject:
                    elements[i] = ConvertForeignObject(foreignObject, options);
                    break;

                case SvgGroup group:
                    ConvertForeignObjectsToNativeText(group.Children, options);
                    break;
            }
        }
    }

    private static SvgElement ConvertForeignObject(SvgForeignObject foreignObject, RenderOptions options)
    {
        var lines = ExtractTextLines(foreignObject.HtmlContent);

        var centerX = foreignObject.X + foreignObject.Width / 2;
        var centerY = foreignObject.Y + foreignObject.Height / 2;

        var fontSizePx = $"{options.FontSize}px";
        var lineHeight = options.FontSize * 1.5;

        var group = new SvgGroup
        {
            Id = foreignObject.Id,
            Class = foreignObject.Class,
            Style = foreignObject.Style,
            Transform = foreignObject.Transform
        };

        if (lines.Count == 0)
        {
            return group;
        }

        if (lines.Count == 1)
        {
            group.Children.Add(new SvgText
            {
                X = centerX,
                Y = centerY,
                Content = lines[0],
                TextAnchor = "middle",
                DominantBaseline = "middle",
                FontSize = fontSizePx,
                FontFamily = options.FontFamily,
                Class = foreignObject.Class
            });

            return group;
        }

        var half = (lines.Count - 1) / 2.0;
        for (var lineIndex = 0; lineIndex < lines.Count; lineIndex++)
        {
            var y = centerY + (lineIndex - half) * lineHeight;
            group.Children.Add(new SvgText
            {
                X = centerX,
                Y = y,
                Content = lines[lineIndex],
                TextAnchor = "middle",
                DominantBaseline = "middle",
                FontSize = fontSizePx,
                FontFamily = options.FontFamily,
                Class = foreignObject.Class
            });
        }

        return group;
    }

    private static List<string> ExtractTextLines(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return [];
        }

        var text = Regex.Replace(html, "<\\s*br\\s*/?>", "\n", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, "</\\s*p\\s*>", "\n", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, "<\\s*p[^>]*>", "", RegexOptions.IgnoreCase);

        text = Regex.Replace(
            text,
            "<\\s*i\\b[^>]*class=\\\"[^\\\"]*\\bfa-([a-z0-9-]+)\\b[^\\\"]*\\\"[^>]*>\\s*</\\s*i\\s*>",
            "$1",
            RegexOptions.IgnoreCase);

        text = Regex.Replace(text, "<[^>]+>", "");

        text = System.Net.WebUtility.HtmlDecode(text);

        var lines = text
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(static _ => _.Trim())
            .Where(static _ => _.Length > 0)
            .ToList();

        return lines;
    }
}
