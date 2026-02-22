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

    public void ToXml(StringBuilder builder)
    {
        builder.Append($"<marker id=\"{Id}\"");

        if (ClassName is not null)
        {
            builder.Append($" class=\"{ClassName}\"");
        }

        if (ViewBox is not null)
        {
            builder.Append($" viewBox=\"{ViewBox}\"");
        }

        builder.Append($" refX=\"{Fmt(RefX)}\" refY=\"{Fmt(RefY)}\"");
        if (MarkerUnits is not null)
        {
            builder.Append($" markerUnits=\"{MarkerUnits}\"");
        }

        builder.Append($" markerWidth=\"{Fmt(MarkerWidth)}\" markerHeight=\"{Fmt(MarkerHeight)}\"");
        builder.Append($" orient=\"{Orient}\">");

        if (UseCircle)
        {
            builder.Append($"<circle cx=\"{Fmt(CircleCx)}\" cy=\"{Fmt(CircleCy)}\" r=\"{Fmt(CircleR)}\" class=\"arrowMarkerPath\" style=\"stroke-width: {StrokeWidth}; stroke-dasharray: 1, 0;\"/>");
        }
        else
        {
            builder.Append($"<path d=\"{Path}\" class=\"arrowMarkerPath\" style=\"stroke-width: {StrokeWidth}; stroke-dasharray: 1, 0;\"/>");
        }

        builder.Append("</marker>");
    }

    static string Fmt(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
}
