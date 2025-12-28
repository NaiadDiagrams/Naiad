namespace MermaidSharp.Rendering;

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
    public string? ViewBox { get; set; }
    public string? MarkerUnits { get; set; }
    public string? ClassName { get; set; }

    public string ToXml()
    {
        var sb = new StringBuilder();
        sb.Append($"<marker id=\"{Id}\"");
        if (ClassName is not null) sb.Append($" class=\"{ClassName}\"");
        if (ViewBox is not null) sb.Append($" viewBox=\"{ViewBox}\"");
        sb.Append($" refX=\"{Fmt(RefX)}\" refY=\"{Fmt(RefY)}\"");
        if (MarkerUnits is not null) sb.Append($" markerUnits=\"{MarkerUnits}\"");
        sb.Append($" markerWidth=\"{Fmt(MarkerWidth)}\" markerHeight=\"{Fmt(MarkerHeight)}\"");
        sb.Append($" orient=\"{Orient}\">");
        sb.Append($"<path d=\"{Path}\" class=\"arrowMarkerPath\" style=\"stroke-width: 1; stroke-dasharray: 1, 0;\"/>");
        sb.Append("</marker>");
        return sb.ToString();
    }

    static string Fmt(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
}
