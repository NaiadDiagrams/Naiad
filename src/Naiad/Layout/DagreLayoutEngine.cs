class DagreLayoutEngine : ILayoutEngine
{
    public LayoutResult Layout(GraphDiagramBase diagram, LayoutOptions options)
    {
        if (diagram.Nodes.Count == 0)
        {
            return new()
            {
                Width = 0,
                Height = 0
            };
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
        CoordinateAssignment.RouteEdges(graph, options.Direction);

        // Undo edge reversals
        Acyclic.Undo(graph);

        // Apply positions back to diagram
        ApplyLayout(graph, diagram, options);

        // Calculate bounds (don't add margin again - positions already include it)
        var width = 0.0;
        var height = 0.0;
        foreach (var node in diagram.Nodes)
        {
            var w = node.Position.X + node.Width / 2;
            var h = node.Position.Y + node.Height / 2;
            if (w > width)
            {
                width = w;
            }

            if (h > height)
            {
                height = h;
            }
        }

        return new()
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
            graph.AddNode(
                new()
                {
                    Id = node.Id,
                    Width = node.Width,
                    Height = node.Height
                });
        }

        foreach (var edge in diagram.Edges)
        {
            graph.AddEdge(
                new()
                {
                    SourceId = edge.SourceId,
                    TargetId = edge.TargetId
                });
        }

        return graph;
    }

    static void ApplyLayout(LayoutGraph graph, GraphDiagramBase diagram, LayoutOptions options)
    {
        // Don't add margin here - let the renderer handle padding
        foreach (var node in diagram.Nodes)
        {
            var layoutNode = graph.GetNode(node.Id);
            if (layoutNode is null)
            {
                continue;
            }

            node.Position = new(layoutNode.X, layoutNode.Y);
        }

        // Build edge lookup for O(1) access instead of O(n) FirstOrDefault per edge
        var edgeLookup = new Dictionary<(string, string), LayoutEdge>(graph.Edges.Count);
        foreach (var le in graph.Edges)
        {
            edgeLookup.TryAdd((le.SourceId, le.TargetId), le);
        }

        Dictionary<(string, string), List<LayoutNode>>? dummyLookup = null;

        foreach (var edge in diagram.Edges)
        {
            edgeLookup.TryGetValue((edge.SourceId, edge.TargetId), out var layoutEdge);

            if (layoutEdge is null)
            {
                // Edge was split by dummy nodes - collect points
                dummyLookup ??= BuildDummyLookup(graph);
                CollectEdgePoints(graph, edge, options, dummyLookup);
            }
            else
            {
                edge.Points.Clear();
                foreach (var point in layoutEdge.Points)
                {
                    edge.Points.Add(new(point.X, point.Y));
                }
            }
        }
    }

    static Dictionary<(string, string), List<LayoutNode>> BuildDummyLookup(LayoutGraph graph)
    {
        var lookup = new Dictionary<(string, string), List<LayoutNode>>();
        foreach (var node in graph.Nodes.Values)
        {
            if (!node.IsDummy)
            {
                continue;
            }

            var key = (node.OriginalEdgeSource ?? "", node.OriginalEdgeTarget ?? "");
            if (!lookup.TryGetValue(key, out var list))
            {
                list = [];
                lookup[key] = list;
            }

            list.Add(node);
        }

        foreach (var list in lookup.Values)
        {
            list.Sort((a, b) => a.Rank.CompareTo(b.Rank));
        }

        return lookup;
    }

    static void CollectEdgePoints(
        LayoutGraph graph,
        Edge edge,
        LayoutOptions options,
        Dictionary<(string, string), List<LayoutNode>> dummyLookup)
    {
        edge.Points.Clear();

        var source = graph.GetNode(edge.SourceId);
        var target = graph.GetNode(edge.TargetId);

        if (source is null || target is null)
        {
            return;
        }

        var isHorizontal = options.Direction is Direction.LeftToRight or Direction.RightToLeft;

        // For horizontal layout: connect right edge of source to left edge of target
        // For vertical layout: connect bottom edge of source to top edge of target
        var sourceEdgeX = isHorizontal ? source.X + source.Width / 2 : source.X;
        var sourceEdgeY = isHorizontal ? source.Y : source.Y + source.Height / 2;
        edge.Points.Add(new(sourceEdgeX, sourceEdgeY));

        if (dummyLookup.TryGetValue((edge.SourceId, edge.TargetId), out var dummies))
        {
            foreach (var dummy in dummies)
            {
                edge.Points.Add(new(dummy.X, dummy.Y));
            }
        }

        var targetEdgeX = isHorizontal ? target.X - target.Width / 2 : target.X;
        var targetEdgeY = isHorizontal ? target.Y : target.Y - target.Height / 2;
        edge.Points.Add(new(targetEdgeX, targetEdgeY));
    }
}
