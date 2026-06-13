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

        // Diagrams with subgraphs are laid out cluster-by-cluster: each
        // subgraph's contents are laid out in isolation and the subgraph is
        // treated as a single composite node in its parent container.
        if (diagram.Subgraphs.Count > 0)
        {
            return LayoutClustered(diagram, options);
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

    const double ClusterPadding = 20;
    const double ClusterLabelHeight = 25;

    /// <summary>
    /// Lays out a diagram that contains subgraphs. Each container (the root, or
    /// one subgraph) is laid out in isolation with the flat engine, treating
    /// child subgraphs as composite nodes; positions are then expanded back to
    /// absolute coordinates and each subgraph's bounds are filled in.
    /// </summary>
    LayoutResult LayoutClustered(GraphDiagramBase diagram, LayoutOptions options)
    {
        var nodeById = new Dictionary<string, Node>();
        foreach (var node in diagram.Nodes)
        {
            nodeById[node.Id] = node;
        }

        // Index the subgraph tree: each node's direct subgraph, each subgraph's
        // parent, and a lookup by id.
        var directSubgraph = new Dictionary<string, Subgraph>();
        var subgraphParent = new Dictionary<string, Subgraph?>();
        var subgraphById = new Dictionary<string, Subgraph>();

        void Index(Subgraph subgraph, Subgraph? parent)
        {
            subgraphById[subgraph.Id] = subgraph;
            subgraphParent[subgraph.Id] = parent;
            foreach (var nodeId in subgraph.NodeIds)
            {
                directSubgraph[nodeId] = subgraph;
            }

            foreach (var child in subgraph.NestedSubgraphs)
            {
                Index(child, subgraph);
            }
        }

        foreach (var subgraph in diagram.Subgraphs)
        {
            Index(subgraph, null);
        }

        // The id of the direct child of the container (a node id, or a child
        // subgraph id) that contains the given node, or null if it is elsewhere.
        string? DirectRepresentative(string nodeId, Subgraph? container)
        {
            var direct = directSubgraph.GetValueOrDefault(nodeId);
            if (direct == container)
            {
                return nodeId;
            }

            var current = direct;
            while (current is not null)
            {
                var parent = subgraphParent[current.Id];
                if (parent == container)
                {
                    return current.Id;
                }

                current = parent;
            }

            return null;
        }

        var clusterSizes = new Dictionary<string, (double w, double h)>();
        var clusterLayouts = new Dictionary<string, ClusterContent>();

        ClusterContent LayoutContainer(Subgraph? container)
        {
            var directNodeIds = container is null
                ? diagram.Nodes.Where(_ => !directSubgraph.ContainsKey(_.Id)).Select(_ => _.Id).ToList()
                : container.NodeIds;
            var childSubgraphs = container is null ? diagram.Subgraphs : container.NestedSubgraphs;

            // Lay out child subgraphs first so their composite sizes are known.
            foreach (var child in childSubgraphs)
            {
                var childContent = LayoutContainer(child);
                clusterLayouts[child.Id] = childContent;
                clusterSizes[child.Id] = (
                    childContent.Width + ClusterPadding * 2,
                    childContent.Height + ClusterPadding * 2 + ClusterLabelHeight);
            }

            var graph = new ClusterGraph();
            foreach (var nodeId in directNodeIds)
            {
                if (nodeById.TryGetValue(nodeId, out var node))
                {
                    graph.AddNode(new() { Id = node.Id, Width = node.Width, Height = node.Height });
                }
            }

            foreach (var child in childSubgraphs)
            {
                var (w, h) = clusterSizes[child.Id];
                graph.AddNode(new() { Id = child.Id, Width = w, Height = h });
            }

            foreach (var edge in diagram.Edges)
            {
                var from = DirectRepresentative(edge.SourceId, container);
                var to = DirectRepresentative(edge.TargetId, container);
                if (from is not null &&
                    to is not null &&
                    from != to &&
                    graph.GetNode(from) is not null &&
                    graph.GetNode(to) is not null)
                {
                    graph.AddEdge(new() { SourceId = from, TargetId = to });
                }
            }

            var content = new ClusterContent();
            if (graph.Nodes.Count == 0)
            {
                content.Width = ClusterLabelHeight;
                content.Height = ClusterLabelHeight;
                return content;
            }

            // Recurse into the flat engine (the temp graph has no subgraphs).
            Layout(graph, options);

            var minX = double.MaxValue;
            var minY = double.MaxValue;
            var maxX = double.MinValue;
            var maxY = double.MinValue;
            foreach (var node in graph.Nodes)
            {
                minX = Math.Min(minX, node.Position.X - node.Width / 2);
                minY = Math.Min(minY, node.Position.Y - node.Height / 2);
                maxX = Math.Max(maxX, node.Position.X + node.Width / 2);
                maxY = Math.Max(maxY, node.Position.Y + node.Height / 2);
            }

            foreach (var node in graph.Nodes)
            {
                content.MemberCenters[node.Id] = (node.Position.X - minX, node.Position.Y - minY);
            }

            content.Width = maxX - minX;
            content.Height = maxY - minY;
            return content;
        }

        void PlaceContainer(Subgraph? container, double originX, double originY)
        {
            if (!clusterLayouts.TryGetValue(container?.Id ?? "", out var content))
            {
                return;
            }

            foreach (var (memberId, center) in content.MemberCenters)
            {
                var centerX = originX + center.X;
                var centerY = originY + center.Y;

                if (subgraphById.TryGetValue(memberId, out var childSubgraph))
                {
                    var (w, h) = clusterSizes[memberId];
                    childSubgraph.Position = new(centerX, centerY);
                    childSubgraph.Width = w;
                    childSubgraph.Height = h;
                    PlaceContainer(
                        childSubgraph,
                        centerX - w / 2 + ClusterPadding,
                        centerY - h / 2 + ClusterLabelHeight + ClusterPadding);
                }
                else if (nodeById.TryGetValue(memberId, out var node))
                {
                    node.Position = new(centerX, centerY);
                }
            }
        }

        clusterLayouts[""] = LayoutContainer(null);
        PlaceContainer(null, 0, 0);

        // Straight edges trimmed to the node borders.
        foreach (var edge in diagram.Edges)
        {
            edge.Points.Clear();
            if (!nodeById.TryGetValue(edge.SourceId, out var source) ||
                !nodeById.TryGetValue(edge.TargetId, out var target))
            {
                continue;
            }

            edge.Points.Add(BorderPoint(source, target.Position));
            edge.Points.Add(BorderPoint(target, source.Position));
        }

        var width = 0.0;
        var height = 0.0;
        foreach (var node in diagram.Nodes)
        {
            width = Math.Max(width, node.Position.X + node.Width / 2);
            height = Math.Max(height, node.Position.Y + node.Height / 2);
        }

        foreach (var subgraph in subgraphById.Values)
        {
            var bounds = subgraph.Bounds;
            width = Math.Max(width, bounds.X + bounds.Width);
            height = Math.Max(height, bounds.Y + bounds.Height);
        }

        return new()
        {
            Width = width,
            Height = height
        };
    }

    /// <summary>Point on a node's rectangular border in the direction of a target point.</summary>
    static Position BorderPoint(Node node, Position toward)
    {
        var dx = toward.X - node.Position.X;
        var dy = toward.Y - node.Position.Y;
        if (dx == 0 && dy == 0)
        {
            return node.Position;
        }

        var tx = dx == 0 ? double.PositiveInfinity : node.Width / 2 / Math.Abs(dx);
        var ty = dy == 0 ? double.PositiveInfinity : node.Height / 2 / Math.Abs(dy);
        var t = Math.Min(tx, ty);
        return new(node.Position.X + dx * t, node.Position.Y + dy * t);
    }

    sealed class ClusterGraph : GraphDiagramBase
    {
    }

    sealed class ClusterContent
    {
        public double Width { get; set; }
        public double Height { get; set; }
        public Dictionary<string, (double X, double Y)> MemberCenters { get; } = new();
    }
}
