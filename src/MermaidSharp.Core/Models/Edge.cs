namespace MermaidSharp.Core.Models;

public class Edge
{
    public required string SourceId { get; init; }
    public required string TargetId { get; init; }
    public string? Label { get; set; }
    public EdgeType Type { get; set; } = EdgeType.Arrow;
    public EdgeStyle LineStyle { get; set; } = EdgeStyle.Solid;
    public Style Style { get; set; } = new();
    public string? CssClass { get; set; }

    // Layout properties (set by layout engine)
    public List<Position> Points { get; } = [];

    public Position LabelPosition
    {
        get
        {
            if (Points.Count == 0) return Position.Zero;
            if (Points.Count == 1) return Points[0];

            // Return midpoint of the edge path
            var midIndex = Points.Count / 2;
            if (Points.Count % 2 == 0)
            {
                var p1 = Points[midIndex - 1];
                var p2 = Points[midIndex];
                return new Position((p1.X + p2.X) / 2, (p1.Y + p2.Y) / 2);
            }
            return Points[midIndex];
        }
    }

    public bool HasArrowHead => Type is EdgeType.Arrow or EdgeType.DottedArrow or EdgeType.ThickArrow
        or EdgeType.BiDirectional or EdgeType.BiDirectionalCircle or EdgeType.BiDirectionalCross;

    public bool HasArrowTail => Type is EdgeType.BiDirectional or EdgeType.BiDirectionalCircle
        or EdgeType.BiDirectionalCross;

    public bool HasCircleEnd => Type is EdgeType.CircleEnd or EdgeType.BiDirectionalCircle;
    public bool HasCrossEnd => Type is EdgeType.CrossEnd or EdgeType.BiDirectionalCross;
}
