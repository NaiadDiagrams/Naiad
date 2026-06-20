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

        // Diagrams with subgraphs are laid out in one global pass over all leaf nodes (so parallel
        // branches spread and cross-subgraph edges route through dummy waypoints), then each subgraph's
        // bounds are derived from the union of its members.
        if (diagram.Subgraphs.Count > 0)
        {
            return LayoutGlobal(diagram, options);
        }

        // Build internal graph
        var graph = BuildLayoutGraph(diagram);

        // Phase 1: Make acyclic
        Acyclic.Run(graph);

        // Phase 2: Assign ranks
        Ranker.Run(graph, options.Ranker);

        // Phase 3: Order nodes within ranks
        Ordering.Run(graph);
        Ordering.EnforceSameRankOrder(graph);

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
                    TargetId = edge.TargetId,
                    RankConstraint = edge.RankConstraint,
                    LabelWidth = edge.LabelWidth,
                    LabelHeight = edge.LabelHeight
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

    const double clusterPadding = 20;
    const double clusterLabelHeight = 25;

    /// <summary>
    /// Lays out a diagram that contains subgraphs in one global pass: all leaf nodes and real edges go
    /// through the flat pipeline (so branches spread horizontally and cross-subgraph edges are
    /// dummy-routed), then each subgraph's box is sized to the union of its members and the whole
    /// diagram is shifted so the outermost cluster padding sits at non-negative coordinates.
    /// </summary>
    static LayoutResult LayoutGlobal(GraphDiagramBase diagram, LayoutOptions options)
    {
        var graph = BuildLayoutGraph(diagram);

        Acyclic.Run(graph);
        // Longest-path ranks every node as early as possible, keeping a cluster's members tight to their
        // backbone. TightTree would instead pull a member down toward a far-away cross-cluster successor
        // (e.g. an error node toward the observability sink it feeds), tearing the cluster apart.
        Ranker.AssignRanks(graph, RankerType.LongestPath);
        ClusterRankConfinement.Run(graph, diagram);
        Ranker.InsertDummyNodes(graph);
        Ordering.Run(graph);
        Ordering.EnforceSameRankOrder(graph);
        ClusterOrdering.Run(graph, diagram, clusterPadding);

        // Reserve vertical room for each cluster's title band / bottom padding so external nodes above or
        // below a cluster clear its box. Only meaningful for top-to-bottom, where the title sits at the top.
        double[]? topInset = null;
        double[]? bottomInset = null;
        if (options.Direction == Direction.TopToBottom)
        {
            (topInset, bottomInset) = ComputeRankInsets(graph, diagram);
        }

        CoordinateAssignment.Run(graph, options.NodeSeparation, options.RankSeparation, options.Direction, topInset, bottomInset);
        CoordinateAssignment.RouteEdges(graph, options.Direction);
        Acyclic.Undo(graph);
        ApplyLayout(graph, diagram, options);

        var nodeById = new Dictionary<string, Node>();
        foreach (var node in diagram.Nodes)
        {
            nodeById[node.Id] = node;
        }

        foreach (var subgraph in diagram.Subgraphs)
        {
            ComputeSubgraphBounds(subgraph, nodeById);
        }

        // Cluster boxes extend above/left of their topmost/leftmost member (padding + the title band), so
        // shift everything to keep all geometry at non-negative coordinates.
        ShiftToPositive(diagram);

        var width = 0.0;
        var height = 0.0;
        foreach (var node in diagram.Nodes)
        {
            width = Math.Max(width, node.Position.X + node.Width / 2);
            height = Math.Max(height, node.Position.Y + node.Height / 2);
        }

        foreach (var subgraph in Flatten(diagram.Subgraphs))
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

    /// <summary>
    /// Per-rank vertical insets that reserve room for cluster title bands (above the rank where a cluster
    /// starts) and bottom padding (below the rank where it ends), so nodes outside a cluster don't collide
    /// with its box. Nested clusters stack, since each contributes its own band.
    /// </summary>
    static (double[] Top, double[] Bottom) ComputeRankInsets(LayoutGraph graph, GraphDiagramBase diagram)
    {
        var maxRank = 0;
        var rankOf = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var node in graph.Nodes.Values)
        {
            maxRank = Math.Max(maxRank, node.Rank);
            if (!node.IsDummy && !node.IsClusterBorder)
            {
                rankOf[node.Id] = node.Rank;
            }
        }

        var top = new double[maxRank + 1];
        var bottom = new double[maxRank + 1];

        foreach (var subgraph in Flatten(diagram.Subgraphs))
        {
            var minRank = int.MaxValue;
            var maxMemberRank = int.MinValue;
            foreach (var memberId in AllMemberIds(subgraph))
            {
                if (rankOf.TryGetValue(memberId, out var rank))
                {
                    minRank = Math.Min(minRank, rank);
                    maxMemberRank = Math.Max(maxMemberRank, rank);
                }
            }

            if (minRank == int.MaxValue)
            {
                continue;
            }

            top[minRank] += clusterPadding + clusterLabelHeight;
            bottom[maxMemberRank] += clusterPadding;
        }

        return (top, bottom);
    }

    static IEnumerable<string> AllMemberIds(Subgraph subgraph)
    {
        foreach (var nodeId in subgraph.NodeIds)
        {
            yield return nodeId;
        }

        foreach (var nested in subgraph.NestedSubgraphs)
        {
            foreach (var nodeId in AllMemberIds(nested))
            {
                yield return nodeId;
            }
        }
    }

    /// <summary>
    /// Sizes a subgraph (and, recursively, its nested subgraphs) to enclose the union of its member node
    /// boxes and nested-subgraph boxes, padded on all sides with an extra band at the top for the title.
    /// </summary>
    static void ComputeSubgraphBounds(Subgraph subgraph, Dictionary<string, Node> nodeById)
    {
        var minX = double.PositiveInfinity;
        var minY = double.PositiveInfinity;
        var maxX = double.NegativeInfinity;
        var maxY = double.NegativeInfinity;

        foreach (var nested in subgraph.NestedSubgraphs)
        {
            ComputeSubgraphBounds(nested, nodeById);
            var bounds = nested.Bounds;
            minX = Math.Min(minX, bounds.X);
            minY = Math.Min(minY, bounds.Y);
            maxX = Math.Max(maxX, bounds.X + bounds.Width);
            maxY = Math.Max(maxY, bounds.Y + bounds.Height);
        }

        foreach (var nodeId in subgraph.NodeIds)
        {
            if (!nodeById.TryGetValue(nodeId, out var node))
            {
                continue;
            }

            minX = Math.Min(minX, node.Position.X - node.Width / 2);
            minY = Math.Min(minY, node.Position.Y - node.Height / 2);
            maxX = Math.Max(maxX, node.Position.X + node.Width / 2);
            maxY = Math.Max(maxY, node.Position.Y + node.Height / 2);
        }

        // An empty subgraph has no members to bound; give it a small placeholder box.
        if (double.IsInfinity(minX))
        {
            minX = 0;
            minY = 0;
            maxX = clusterLabelHeight;
            maxY = clusterLabelHeight;
        }

        minX -= clusterPadding;
        maxX += clusterPadding;
        maxY += clusterPadding;
        minY -= clusterPadding + clusterLabelHeight;

        subgraph.Width = maxX - minX;
        subgraph.Height = maxY - minY;
        subgraph.Position = new((minX + maxX) / 2, (minY + maxY) / 2);
    }

    /// <summary>Translates every node, edge point and subgraph so all geometry has non-negative coordinates.</summary>
    static void ShiftToPositive(GraphDiagramBase diagram)
    {
        var minX = double.PositiveInfinity;
        var minY = double.PositiveInfinity;

        foreach (var node in diagram.Nodes)
        {
            minX = Math.Min(minX, node.Position.X - node.Width / 2);
            minY = Math.Min(minY, node.Position.Y - node.Height / 2);
        }

        foreach (var subgraph in Flatten(diagram.Subgraphs))
        {
            var bounds = subgraph.Bounds;
            minX = Math.Min(minX, bounds.X);
            minY = Math.Min(minY, bounds.Y);
        }

        foreach (var edge in diagram.Edges)
        {
            foreach (var point in edge.Points)
            {
                minX = Math.Min(minX, point.X);
                minY = Math.Min(minY, point.Y);
            }
        }

        if (double.IsInfinity(minX))
        {
            return;
        }

        var dx = minX < 0 ? -minX : 0;
        var dy = minY < 0 ? -minY : 0;
        if (dx == 0 && dy == 0)
        {
            return;
        }

        foreach (var node in diagram.Nodes)
        {
            node.Position = new(node.Position.X + dx, node.Position.Y + dy);
        }

        foreach (var subgraph in Flatten(diagram.Subgraphs))
        {
            subgraph.Position = new(subgraph.Position.X + dx, subgraph.Position.Y + dy);
        }

        foreach (var edge in diagram.Edges)
        {
            for (var i = 0; i < edge.Points.Count; i++)
            {
                edge.Points[i] = new(edge.Points[i].X + dx, edge.Points[i].Y + dy);
            }
        }
    }

    static IEnumerable<Subgraph> Flatten(IEnumerable<Subgraph> subgraphs)
    {
        foreach (var subgraph in subgraphs)
        {
            yield return subgraph;
            foreach (var nested in Flatten(subgraph.NestedSubgraphs))
            {
                yield return nested;
            }
        }
    }
}
