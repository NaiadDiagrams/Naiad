namespace Naiad.Models;

/// <summary>
/// Per-element visual overrides resolved from <c>classDef</c>/<c>class</c>/<c>style</c>
/// directives. Any property left null falls back to the renderer's theme default.
/// </summary>
public class NodeStyle
{
    public string? Fill { get; set; }
    public string? Stroke { get; set; }
    public double? StrokeWidth { get; set; }

    /// <summary>Text colour.</summary>
    public string? Color { get; set; }
    public string? StrokeDasharray { get; set; }

    /// <summary>Returns a copy of this style with <paramref name="other"/>'s set properties layered on top.</summary>
    public NodeStyle MergedWith(NodeStyle other) =>
        new()
        {
            Fill = other.Fill ?? Fill,
            Stroke = other.Stroke ?? Stroke,
            StrokeWidth = other.StrokeWidth ?? StrokeWidth,
            Color = other.Color ?? Color,
            StrokeDasharray = other.StrokeDasharray ?? StrokeDasharray
        };
}
