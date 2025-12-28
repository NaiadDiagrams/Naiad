namespace MermaidSharp.Rendering;

public class SvgDocument
{
    public double Width { get; set; }
    public double Height { get; set; }
    public string? ViewBoxOverride { get; set; }
    public string ViewBox => ViewBoxOverride ?? $"0 0 {FmtWidth(Width)} {Fmt(Height)}";
    public List<SvgElement> Elements { get; } = [];
    public SvgDefs Defs { get; } = new();
    public string? CssStyles { get; set; }

    // Mermaid.ink compatibility properties
    public string Id { get; set; } = "mermaid-svg";
    public string? DiagramClass { get; set; }
    public string? AriaRoledescription { get; set; }
    public string? Role { get; set; } = "graphics-document document";
    public string? FontAwesomeImport { get; set; } = "@import url(\"https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.7.2/css/all.min.css\");";

    public string ToXml()
    {
        var sb = new StringBuilder();

        // Build mermaid-compatible SVG root element (attribute order matches mermaid.ink exactly)
        sb.Append($"<svg id=\"{Id}\" width=\"100%\" xmlns=\"http://www.w3.org/2000/svg\"");

        if (!string.IsNullOrEmpty(DiagramClass))
        {
            sb.Append($" class=\"{DiagramClass}\"");
        }

        sb.Append($" viewBox=\"{ViewBox}\"");
        sb.Append($" style=\"max-width: {FmtWidth(Width)}px;\"");

        if (!string.IsNullOrEmpty(Role))
        {
            sb.Append($" role=\"{Role}\"");
        }

        if (!string.IsNullOrEmpty(AriaRoledescription))
        {
            sb.Append($" aria-roledescription=\"{AriaRoledescription}\"");
        }

        sb.Append(" xmlns:xlink=\"http://www.w3.org/1999/xlink\">");

        // Font Awesome import
        if (!string.IsNullOrEmpty(FontAwesomeImport))
        {
            sb.Append($"<style xmlns=\"http://www.w3.org/1999/xhtml\">{FontAwesomeImport}</style>");
        }

        // Main CSS styles
        if (CssStyles is not null)
        {
            sb.Append($"<style>{CssStyles}</style>");
        }

        if (Defs.HasContent)
        {
            sb.Append(Defs.ToXml());
        }

        foreach (var element in Elements)
        {
            sb.Append(element.ToXml());
        }

        sb.Append("</svg>");
        return sb.ToString();
    }

    static string Fmt(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
    static string FmtWidth(double value) => value.ToString("0.####", CultureInfo.InvariantCulture);
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