namespace MermaidSharp.Rendering;

public abstract class SvgElement
{
    public string? Id { get; set; }
    public string? Class { get; set; }
    public string? Style { get; set; }
    public string? Transform { get; set; }

    public abstract string ToXml();

    protected string CommonAttributes()
    {
        var sb = new StringBuilder();
        if (Id is not null) sb.Append($" id=\"{Id}\"");
        if (Class is not null) sb.Append($" class=\"{Class}\"");
        if (Style is not null) sb.Append($" style=\"{Style}\"");
        if (Transform is not null) sb.Append($" transform=\"{Transform}\"");
        return sb.ToString();
    }

    protected static string Fmt(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
}

public class SvgGroup : SvgElement
{
    public List<SvgElement> Children { get; } = [];

    public override string ToXml()
    {
        var sb = new StringBuilder();
        sb.Append($"<g{CommonAttributes()}");
        if (Children.Count == 0)
        {
            sb.Append("/>");
        }
        else
        {
            sb.Append('>');
            foreach (var child in Children)
            {
                sb.Append(child.ToXml());
            }
            sb.Append("</g>");
        }
        return sb.ToString();
    }
}

public class SvgRect : SvgElement
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public double Rx { get; set; }
    public double Ry { get; set; }
    public string? Fill { get; set; }
    public string? Stroke { get; set; }
    public double? StrokeWidth { get; set; }

    public override string ToXml()
    {
        var sb = new StringBuilder();
        sb.Append($"<rect x=\"{Fmt(X)}\" y=\"{Fmt(Y)}\" width=\"{Fmt(Width)}\" height=\"{Fmt(Height)}\"");
        if (Rx > 0) sb.Append($" rx=\"{Fmt(Rx)}\"");
        if (Ry > 0) sb.Append($" ry=\"{Fmt(Ry)}\"");
        if (Fill is not null) sb.Append($" fill=\"{Fill}\"");
        if (Stroke is not null) sb.Append($" stroke=\"{Stroke}\"");
        if (StrokeWidth.HasValue) sb.Append($" stroke-width=\"{Fmt(StrokeWidth.Value)}\"");
        sb.Append(CommonAttributes());
        sb.Append("/>");
        return sb.ToString();
    }
}

public class SvgRectNoXY : SvgElement
{
    public double Width { get; set; }
    public double Height { get; set; }

    public override string ToXml()
    {
        var sb = new StringBuilder();
        sb.Append($"<rect width=\"{Fmt(Width)}\" height=\"{Fmt(Height)}\"");
        sb.Append(CommonAttributes());
        sb.Append("/>");
        return sb.ToString();
    }
}

public class SvgCircle : SvgElement
{
    public double Cx { get; set; }
    public double Cy { get; set; }
    public double R { get; set; }
    public string? Fill { get; set; }
    public string? Stroke { get; set; }
    public double? StrokeWidth { get; set; }

    public override string ToXml()
    {
        var builder = new StringBuilder();
        builder.Append($"<circle cx=\"{Fmt(Cx)}\" cy=\"{Fmt(Cy)}\" r=\"{Fmt(R)}\"");
        if (Fill is not null) builder.Append($" fill=\"{Fill}\"");
        if (Stroke is not null) builder.Append($" stroke=\"{Stroke}\"");
        if (StrokeWidth.HasValue) builder.Append($" stroke-width=\"{Fmt(StrokeWidth.Value)}\"");
        builder.Append(CommonAttributes());
        builder.Append("/>");
        return builder.ToString();
    }
}

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
        var sb = new StringBuilder();
        sb.Append($"<ellipse cx=\"{Fmt(Cx)}\" cy=\"{Fmt(Cy)}\" rx=\"{Fmt(Rx)}\" ry=\"{Fmt(Ry)}\"");
        if (Fill is not null) sb.Append($" fill=\"{Fill}\"");
        if (Stroke is not null) sb.Append($" stroke=\"{Stroke}\"");
        sb.Append(CommonAttributes());
        sb.Append("/>");
        return sb.ToString();
    }
}

public class SvgLine : SvgElement
{
    public double X1 { get; set; }
    public double Y1 { get; set; }
    public double X2 { get; set; }
    public double Y2 { get; set; }
    public string? Stroke { get; set; }
    public double? StrokeWidth { get; set; }
    public string? StrokeDasharray { get; set; }

    public override string ToXml()
    {
        var sb = new StringBuilder();
        sb.Append($"<line x1=\"{Fmt(X1)}\" y1=\"{Fmt(Y1)}\" x2=\"{Fmt(X2)}\" y2=\"{Fmt(Y2)}\"");
        if (Stroke is not null) sb.Append($" stroke=\"{Stroke}\"");
        if (StrokeWidth.HasValue) sb.Append($" stroke-width=\"{Fmt(StrokeWidth.Value)}\"");
        if (StrokeDasharray is not null) sb.Append($" stroke-dasharray=\"{StrokeDasharray}\"");
        sb.Append(CommonAttributes());
        sb.Append("/>");
        return sb.ToString();
    }
}

public class SvgPath : SvgElement
{
    public required string D { get; set; }
    public string? Fill { get; set; }
    public string? Stroke { get; set; }
    public double? StrokeWidth { get; set; }
    public string? StrokeDasharray { get; set; }
    public string? MarkerStart { get; set; }
    public string? MarkerEnd { get; set; }
    public double? Opacity { get; set; }

    public override string ToXml()
    {
        var sb = new StringBuilder();
        sb.Append($"<path d=\"{D}\"");
        if (Fill is not null) sb.Append($" fill=\"{Fill}\"");
        if (Stroke is not null) sb.Append($" stroke=\"{Stroke}\"");
        if (StrokeWidth.HasValue) sb.Append($" stroke-width=\"{Fmt(StrokeWidth.Value)}\"");
        if (StrokeDasharray is not null) sb.Append($" stroke-dasharray=\"{StrokeDasharray}\"");
        if (MarkerStart is not null) sb.Append($" marker-start=\"{MarkerStart}\"");
        if (MarkerEnd is not null) sb.Append($" marker-end=\"{MarkerEnd}\"");
        if (Opacity.HasValue) sb.Append($" opacity=\"{Fmt(Opacity.Value)}\"");
        sb.Append(CommonAttributes());
        sb.Append("/>");
        return sb.ToString();
    }
}

public class SvgPolygon : SvgElement
{
    public List<Position> Points { get; } = [];
    public string? Fill { get; set; }
    public string? Stroke { get; set; }

    public override string ToXml()
    {
        var pointsStr = string.Join(" ", Points.Select(p => $"{Fmt(p.X)},{Fmt(p.Y)}"));
        var sb = new StringBuilder();
        sb.Append($"<polygon points=\"{pointsStr}\"");
        if (Fill is not null) sb.Append($" fill=\"{Fill}\"");
        if (Stroke is not null) sb.Append($" stroke=\"{Stroke}\"");
        sb.Append(CommonAttributes());
        sb.Append("/>");
        return sb.ToString();
    }
}

public class SvgPolyline : SvgElement
{
    public List<Position> Points { get; } = [];
    public string? Fill { get; set; }
    public string? Stroke { get; set; }
    public double? StrokeWidth { get; set; }
    public string? StrokeDasharray { get; set; }
    public string? MarkerEnd { get; set; }

    public override string ToXml()
    {
        var pointsStr = string.Join(" ", Points.Select(p => $"{Fmt(p.X)},{Fmt(p.Y)}"));
        var sb = new StringBuilder();
        sb.Append($"<polyline points=\"{pointsStr}\"");
        if (Fill is not null) sb.Append($" fill=\"{Fill}\"");
        if (Stroke is not null) sb.Append($" stroke=\"{Stroke}\"");
        if (StrokeWidth.HasValue) sb.Append($" stroke-width=\"{Fmt(StrokeWidth.Value)}\"");
        if (StrokeDasharray is not null) sb.Append($" stroke-dasharray=\"{StrokeDasharray}\"");
        if (MarkerEnd is not null) sb.Append($" marker-end=\"{MarkerEnd}\"");
        sb.Append(CommonAttributes());
        sb.Append("/>");
        return sb.ToString();
    }
}

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
        var sb = new StringBuilder();
        sb.Append("<text");

        // For transformed text (OmitXY=true), mermaid.ink uses: transform, class, style order
        if (OmitXY)
        {
            if (Transform is not null) sb.Append($" transform=\"{Transform}\"");
            if (Class is not null) sb.Append($" class=\"{Class}\"");
            if (Style is not null) sb.Append($" style=\"{Style}\"");
        }
        else
        {
            sb.Append($" x=\"{Fmt(X)}\" y=\"{Fmt(Y)}\"");
            if (TextAnchor is not null) sb.Append($" text-anchor=\"{TextAnchor}\"");
            if (DominantBaseline is not null) sb.Append($" dominant-baseline=\"{DominantBaseline}\"");
            if (FontSize is not null) sb.Append($" font-size=\"{FontSize}\"");
            if (FontFamily is not null) sb.Append($" font-family=\"{FontFamily}\"");
            if (FontWeight is not null) sb.Append($" font-weight=\"{FontWeight}\"");
            if (Fill is not null) sb.Append($" fill=\"{Fill}\"");
            sb.Append(CommonAttributes());
        }

        if (string.IsNullOrEmpty(Content))
        {
            sb.Append("/>");
        }
        else
        {
            sb.Append($">{EscapeXml(Content)}</text>");
        }
        return sb.ToString();
    }

    static string EscapeXml(string text) =>
        text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&apos;");
}

public class SvgForeignObject : SvgElement
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public required string HtmlContent { get; set; }

    public override string ToXml()
    {
        var sb = new StringBuilder();
        sb.Append($"<foreignObject x=\"{Fmt(X)}\" y=\"{Fmt(Y)}\" width=\"{Fmt(Width)}\" height=\"{Fmt(Height)}\"");
        sb.Append(CommonAttributes());
        sb.Append('>');
        sb.Append($"<div xmlns=\"http://www.w3.org/1999/xhtml\" style=\"display: table-cell; white-space: nowrap; line-height: 1.5; max-width: 200px; text-align: center; vertical-align: middle; width: {Fmt(Width)}px; height: {Fmt(Height)}px;\">");
        sb.Append($"<span class=\"nodeLabel\">{HtmlContent}</span>");
        sb.Append("</div>");
        sb.Append("</foreignObject>");
        return sb.ToString();
    }
}
