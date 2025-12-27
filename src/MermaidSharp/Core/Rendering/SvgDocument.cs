using System.Text;

namespace MermaidSharp.Rendering;

public class SvgDocument
{
    public double Width { get; set; }
    public double Height { get; set; }
    public string ViewBox => $"0 0 {Fmt(Width)} {Fmt(Height)}";
    public List<SvgElement> Elements { get; } = [];
    public SvgDefs Defs { get; } = new();
    public string? CssStyles { get; set; }

    public string ToXml()
    {
        var sb = new StringBuilder();
        sb.Append($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{Fmt(Width)}\" height=\"{Fmt(Height)}\" viewBox=\"{ViewBox}\">");

        if (Defs.HasContent)
        {
            sb.Append(Defs.ToXml());
        }

        if (CssStyles is not null)
        {
            sb.Append($"<style>{CssStyles}</style>");
        }

        foreach (var element in Elements)
        {
            sb.Append(element.ToXml());
        }

        sb.Append("</svg>");
        return sb.ToString();
    }

    static string Fmt(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
}

public class SvgDefs
{
    public List<SvgMarker> Markers { get; } = [];
    public List<SvgGradient> Gradients { get; } = [];
    public List<SvgFilter> Filters { get; } = [];

    public bool HasContent => Markers.Count > 0 || Gradients.Count > 0 || Filters.Count > 0;

    public string ToXml()
    {
        var sb = new StringBuilder();
        sb.Append("<defs>");

        foreach (var marker in Markers)
        {
            sb.Append(marker.ToXml());
        }

        foreach (var gradient in Gradients)
        {
            sb.Append(gradient.ToXml());
        }

        foreach (var filter in Filters)
        {
            sb.Append(filter.ToXml());
        }

        sb.Append("</defs>");
        return sb.ToString();
    }
}

public class SvgMarker
{
    public required string Id { get; init; }
    public required string Path { get; init; }
    public double MarkerWidth { get; set; } = 10;
    public double MarkerHeight { get; set; } = 7;
    public double RefX { get; set; } = 9;
    public double RefY { get; set; } = 3.5;
    public string? Fill { get; set; }
    public string Orient { get; set; } = "auto";

    public string ToXml()
    {
        var fill = Fill ?? "#333";
        return $"<marker id=\"{Id}\" markerWidth=\"{Fmt(MarkerWidth)}\" markerHeight=\"{Fmt(MarkerHeight)}\" " +
               $"refX=\"{Fmt(RefX)}\" refY=\"{Fmt(RefY)}\" orient=\"{Orient}\">" +
               $"<path d=\"{Path}\" fill=\"{fill}\" />" +
               "</marker>";
    }

    static string Fmt(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
}

public class SvgGradient
{
    public required string Id { get; init; }
    public List<SvgGradientStop> Stops { get; } = [];
    public bool IsRadial { get; set; }

    public string ToXml()
    {
        var tag = IsRadial ? "radialGradient" : "linearGradient";
        var sb = new StringBuilder();
        sb.Append($"<{tag} id=\"{Id}\">");
        foreach (var stop in Stops)
        {
            sb.Append($"<stop offset=\"{stop.Offset}%\" style=\"stop-color:{stop.Color}\" />");
        }
        sb.Append($"</{tag}>");
        return sb.ToString();
    }
}

public class SvgGradientStop
{
    public double Offset { get; set; }
    public required string Color { get; init; }
}

public class SvgFilter
{
    public required string Id { get; init; }
    public required string Content { get; init; }

    public string ToXml() => $"<filter id=\"{Id}\">{Content}</filter>";
}
