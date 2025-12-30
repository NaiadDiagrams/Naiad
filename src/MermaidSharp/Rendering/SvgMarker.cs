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
    public bool UseCircle { get; set; }
    public double CircleCx { get; set; } = 5;
    public double CircleCy { get; set; } = 5;
    public double CircleR { get; set; } = 5;
    public int StrokeWidth { get; set; } = 1;

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

        if (UseCircle)
        {
            sb.Append($"<circle cx=\"{Fmt(CircleCx)}\" cy=\"{Fmt(CircleCy)}\" r=\"{Fmt(CircleR)}\" class=\"arrowMarkerPath\" style=\"stroke-width: {StrokeWidth}; stroke-dasharray: 1, 0;\"/>");
        }
        else
        {
            sb.Append($"<path d=\"{Path}\" class=\"arrowMarkerPath\" style=\"stroke-width: {StrokeWidth}; stroke-dasharray: 1, 0;\"/>");
        }

        sb.Append("</marker>");
        return sb.ToString();
    }

    static string Fmt(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
}
