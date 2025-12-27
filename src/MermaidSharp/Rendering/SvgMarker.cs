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