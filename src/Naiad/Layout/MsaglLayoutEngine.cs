namespace MermaidSharp.Layout;

using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Core.Routing;
using Microsoft.Msagl.Layout.Layered;
using Microsoft.Msagl.Miscellaneous;
using MermaidSharp.Models;
using Direction = MermaidSharp.Models.Direction;
using MsaglEdge = Microsoft.Msagl.Core.Layout.Edge;
using MsaglNode = Microsoft.Msagl.Core.Layout.Node;

public class MsaglLayoutEngine : ILayoutEngine
{
    public LayoutResult Layout(GraphDiagramBase diagram, LayoutOptions options)
    {
        if (diagram.Nodes.Count == 0)
        {
            return new() { Width = 0, Height = 0 };
        }

        // Build MSAGL GeometryGraph
        var (graph, nodeMap) = BuildGeometryGraph(diagram);

        // Configure Sugiyama settings
        var settings = CreateSugiyamaSettings(options);

        // Execute layout
        LayoutHelpers.CalculateLayout(graph, settings, null);

        // Apply results back to diagram
        ApplyLayout(graph, nodeMap, diagram, options);

        // Calculate bounds (margins already applied via offset, renderers add extra space as needed)
        var width = diagram.Nodes.Max(n => n.Position.X + n.Width / 2);
        var height = diagram.Nodes.Max(n => n.Position.Y + n.Height / 2);

        return new()
        {
            Width = width,
            Height = height
        };
    }

    static (GeometryGraph graph, Dictionary<string, MsaglNode> nodeMap) BuildGeometryGraph(GraphDiagramBase diagram)
    {
        var graph = new GeometryGraph();
        var nodeMap = new Dictionary<string, MsaglNode>();

        // Create MSAGL nodes
        foreach (var naiadNode in diagram.Nodes)
        {
            var msaglNode = new MsaglNode(
                CurveFactory.CreateRectangle(
                    naiadNode.Width,
                    naiadNode.Height,
                    new Point(0, 0)),
                naiadNode.Id);

            graph.Nodes.Add(msaglNode);
            nodeMap[naiadNode.Id] = msaglNode;
        }

        // Create MSAGL edges
        foreach (var naiadEdge in diagram.Edges)
        {
            if (nodeMap.TryGetValue(naiadEdge.SourceId, out var sourceNode) &&
                nodeMap.TryGetValue(naiadEdge.TargetId, out var targetNode))
            {
                var msaglEdge = new MsaglEdge(sourceNode, targetNode);
                graph.Edges.Add(msaglEdge);
            }
        }

        return (graph, nodeMap);
    }

    static SugiyamaLayoutSettings CreateSugiyamaSettings(LayoutOptions options)
    {
        var settings = new SugiyamaLayoutSettings
        {
            NodeSeparation = options.NodeSeparation,
            LayerSeparation = options.RankSeparation,
            Transformation = GetTransformation(options.Direction),
            EdgeRoutingSettings =
            {
                EdgeRoutingMode = EdgeRoutingMode.StraightLine
            }
        };

        return settings;
    }

    // MSAGL default is bottom-to-top
    static PlaneTransformation GetTransformation(Direction direction) =>
        direction switch
        {
            Direction.BottomToTop => PlaneTransformation.UnitTransformation,
            Direction.TopToBottom => new PlaneTransformation(-1, 0, 0, 0, -1, 0),
            Direction.LeftToRight => new PlaneTransformation(0, -1, 0, 1, 0, 0),
            Direction.RightToLeft => new PlaneTransformation(0, 1, 0, -1, 0, 0),
            _ => PlaneTransformation.UnitTransformation
        };

    static void ApplyLayout(
        GeometryGraph graph,
        Dictionary<string, MsaglNode> nodeMap,
        GraphDiagramBase diagram,
        LayoutOptions options)
    {
        // Normalize positions to start from origin + margins
        var bounds = graph.BoundingBox;
        var offsetX = -bounds.Left + options.MarginX;
        var offsetY = -bounds.Bottom + options.MarginY;

        // Apply node positions
        foreach (var naiadNode in diagram.Nodes)
        {
            if (nodeMap.TryGetValue(naiadNode.Id, out var msaglNode))
            {
                // MSAGL uses center-based coordinates (same as Naiad)
                naiadNode.Position = new Position(
                    msaglNode.Center.X + offsetX,
                    msaglNode.Center.Y + offsetY);
            }
        }

        // Apply edge routing points
        foreach (var naiadEdge in diagram.Edges)
        {
            var msaglEdge = FindMsaglEdge(graph, naiadEdge.SourceId, naiadEdge.TargetId);
            if (msaglEdge?.Curve != null)
            {
                naiadEdge.Points.Clear();
                ExtractEdgePoints(msaglEdge, naiadEdge.Points, offsetX, offsetY);
            }
        }
    }

    static MsaglEdge? FindMsaglEdge(GeometryGraph graph, string sourceId, string targetId) =>
        graph.Edges.FirstOrDefault(e =>
            e.Source.UserData as string == sourceId &&
            e.Target.UserData as string == targetId);

    static void ExtractEdgePoints(MsaglEdge msaglEdge, List<Position> points, double offsetX, double offsetY)
    {
        var curve = msaglEdge.Curve;

        // Add source connection point if available
        if (msaglEdge.EdgeGeometry?.SourcePort != null)
        {
            var sourcePoint = msaglEdge.EdgeGeometry.SourcePort.Location;
            points.Add(new Position(sourcePoint.X + offsetX, sourcePoint.Y + offsetY));
        }
        else
        {
            points.Add(new Position(curve.Start.X + offsetX, curve.Start.Y + offsetY));
        }

        // Extract intermediate points from curve
        switch (curve)
        {
            case LineSegment:
                // For straight lines, start and end are sufficient
                break;

            case Polyline polyline:
                // Skip first and last as they're handled separately
                var polyPoints = polyline.PolylinePoints.ToList();
                for (var i = 1; i < polyPoints.Count - 1; i++)
                {
                    var pt = polyPoints[i];
                    points.Add(new Position(pt.Point.X + offsetX, pt.Point.Y + offsetY));
                }
                break;

            case Curve compositeCurve:
                // Sample composite curves
                var segments = compositeCurve.Segments.ToList();
                foreach (var segment in segments)
                {
                    // Add segment end point (start is previous end)
                    if (segment != segments.Last())
                    {
                        points.Add(new Position(
                            segment.End.X + offsetX,
                            segment.End.Y + offsetY));
                    }
                }
                break;

            default:
                // Fallback: sample the curve
                const int sampleCount = 5;
                for (var i = 1; i < sampleCount; i++)
                {
                    var t = curve.ParStart + (curve.ParEnd - curve.ParStart) * i / sampleCount;
                    var p = curve[t];
                    points.Add(new Position(p.X + offsetX, p.Y + offsetY));
                }
                break;
        }

        // Add target connection point
        if (msaglEdge.EdgeGeometry?.TargetPort != null)
        {
            var targetPoint = msaglEdge.EdgeGeometry.TargetPort.Location;
            points.Add(new Position(targetPoint.X + offsetX, targetPoint.Y + offsetY));
        }
        else
        {
            points.Add(new Position(curve.End.X + offsetX, curve.End.Y + offsetY));
        }
    }
}
