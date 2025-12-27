using MermaidSharp.Layout.Dagre;

namespace MermaidSharp.Layout;

public class DagreLayoutEngine : ILayoutEngine
{
    public LayoutResult Layout(GraphDiagramBase diagram, LayoutOptions options)
    {
        if (diagram.Nodes.Count == 0)
        {
            return new LayoutResult { Width = 0, Height = 0 };
        }

        // Build internal graph
        var graph = BuildLayoutGraph(diagram);

        // Phase 1: Make acyclic
        Acyclic.Run(graph);

        // Phase 2: Assign ranks
        Ranker.Run(graph, options.Ranker);

        // Phase 3: Order nodes within ranks
        Ordering.Run(graph);

        // Phase 4: Assign coordinates
        CoordinateAssignment.Run(graph, options.NodeSeparation, options.RankSeparation, options.Direction);

        // Phase 5: Route edges
        CoordinateAssignment.RouteEdges(graph);

        // Undo edge reversals
        Acyclic.Undo(graph);

        // Apply positions back to diagram
        ApplyLayout(graph, diagram, options);

        // Calculate bounds
        var width = diagram.Nodes.Max(n => n.Position.X + n.Width / 2) + options.MarginX;
        var height = diagram.Nodes.Max(n => n.Position.Y + n.Height / 2) + options.MarginY;

        return new LayoutResult
        {
            Width = width,
            Height = height
        };
    }

    static LayoutGraph BuildLayoutGraph(GraphDiagramBase diagram)
    {
        var graph = new LayoutGraph();

        foreach (var node in diagram.Nodes)
        {
            graph.AddNode(new LayoutNode
            {
                Id = node.Id,
                Width = node.Width,
                Height = node.Height
            });
        }

        foreach (var edge in diagram.Edges)
        {
            graph.AddEdge(new LayoutEdge
            {
                SourceId = edge.SourceId,
                TargetId = edge.TargetId
            });
        }

        return graph;
    }

    static void ApplyLayout(LayoutGraph graph, GraphDiagramBase diagram, LayoutOptions options)
    {
        foreach (var node in diagram.Nodes)
        {
            var layoutNode = graph.GetNode(node.Id);
            if (layoutNode is not null)
            {
                node.Position = new Position(
                    layoutNode.X + options.MarginX,
                    layoutNode.Y + options.MarginY);
                node.Rank = layoutNode.Rank;
                node.Order = layoutNode.Order;
            }
        }

        foreach (var edge in diagram.Edges)
        {
            // Find the original edge or reconstruct from dummies
            var layoutEdge = graph.Edges.FirstOrDefault(e =>
                e.SourceId == edge.SourceId && e.TargetId == edge.TargetId);

            if (layoutEdge is not null)
            {
                edge.Points.Clear();
                foreach (var point in layoutEdge.Points)
                {
                    edge.Points.Add(new Position(
                        point.X + options.MarginX,
                        point.Y + options.MarginY));
                }
            }
            else
            {
                // Edge was split by dummy nodes - collect points
                CollectEdgePoints(graph, edge, options);
            }
        }
    }

    static void CollectEdgePoints(LayoutGraph graph, Edge edge, LayoutOptions options)
    {
        edge.Points.Clear();

        var source = graph.GetNode(edge.SourceId);
        var target = graph.GetNode(edge.TargetId);

        if (source is null || target is null) return;

        edge.Points.Add(new Position(
            source.X + options.MarginX,
            source.Y + source.Height / 2 + options.MarginY));

        // Find dummy nodes
        var dummies = graph.Nodes.Values
            .Where(n => n.IsDummy &&
                        n.OriginalEdgeSource == edge.SourceId &&
                        n.OriginalEdgeTarget == edge.TargetId)
            .OrderBy(n => n.Rank)
            .ToList();

        foreach (var dummy in dummies)
        {
            edge.Points.Add(new Position(
                dummy.X + options.MarginX,
                dummy.Y + options.MarginY));
        }

        edge.Points.Add(new Position(
            target.X + options.MarginX,
            target.Y - target.Height / 2 + options.MarginY));
    }
}
