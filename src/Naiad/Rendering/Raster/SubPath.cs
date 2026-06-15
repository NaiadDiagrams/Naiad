/// <summary>
/// A single flattened contour produced by <see cref="PathFlattener"/>: a run of points plus whether
/// the contour closes back to its start. Curves and arcs in the source path data are already
/// approximated as line segments here, so every render surface only ever has to deal with polylines —
/// the curve-flattening work is shared in the core walker rather than re-done per backend.
/// </summary>
sealed class SubPath(List<Vector2> points, bool closed)
{
    public List<Vector2> Points { get; } = points;

    public bool Closed { get; } = closed;
}

enum FillRule
{
    NonZero,
    EvenOdd,
}
