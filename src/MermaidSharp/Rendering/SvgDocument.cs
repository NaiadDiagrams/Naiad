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