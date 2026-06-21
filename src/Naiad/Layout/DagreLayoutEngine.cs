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

        // Replace each top-level cluster's (possibly sheared) global layout with a compact isolated layout
        // dropped at the cluster's centroid, then push overlapping boxes apart — guaranteeing the cluster
        // boxes don't overlap while keeping the overall arrangement the global pass chose.
        CompactClusters(diagram, options, nodeById);

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

    /// <summary>
    /// Replaces each top-level cluster's global (often sheared) layout with a compact isolated layout
    /// dropped at the cluster's centroid, pushes overlapping cluster boxes apart, and re-routes
    /// cross-cluster edges border-to-border. Intra-cluster edges keep their isolated-layout routing.
    /// </summary>
    static void CompactClusters(GraphDiagramBase diagram, LayoutOptions options, Dictionary<string, Node> nodeById)
    {
        var topCluster = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var subgraph in diagram.Subgraphs)
        {
            foreach (var memberId in AllMemberIds(subgraph))
            {
                topCluster[memberId] = subgraph.Id;
            }
        }

        var offset = new Dictionary<string, (double Dx, double Dy)>(StringComparer.Ordinal);

        foreach (var subgraph in diagram.Subgraphs)
        {
            var memberIds = new HashSet<string>(AllMemberIds(subgraph));
            var members = new List<Node>();
            foreach (var id in memberIds)
            {
                if (nodeById.TryGetValue(id, out var node))
                {
                    members.Add(node);
                }
            }

            if (members.Count == 0)
            {
                continue;
            }

            var centerX = members.Average(_ => _.Position.X);
            var centerY = members.Average(_ => _.Position.Y);

            // Lay the cluster out on its own; with no external edges to pull it, it stays compact.
            var iso = new ClusterGraph { Direction = options.Direction };
            foreach (var node in members)
            {
                iso.Nodes.Add(node);
            }

            foreach (var edge in diagram.Edges)
            {
                if (memberIds.Contains(edge.SourceId) && memberIds.Contains(edge.TargetId))
                {
                    iso.Edges.Add(edge);
                }
            }

            foreach (var nested in subgraph.NestedSubgraphs)
            {
                iso.Subgraphs.Add(nested);
            }

            new DagreLayoutEngine().Layout(iso, options);

            var dx = centerX - members.Average(_ => _.Position.X);
            var dy = centerY - members.Average(_ => _.Position.Y);
            foreach (var node in members)
            {
                node.Position = new(node.Position.X + dx, node.Position.Y + dy);
            }

            offset[subgraph.Id] = (dx, dy);
        }

        SeparateClusterBoxes(diagram, nodeById, topCluster, offset, options.NodeSeparation);

        // Each top-level cluster's rendered box (plus a routing margin) is an obstacle for cross-cluster edges.
        const double routeMargin = 12;
        var clusterBox = new Dictionary<string, BoxRouter.Box>(StringComparer.Ordinal);
        var grouped = new Dictionary<string, List<Node>>(StringComparer.Ordinal);
        foreach (var (nodeId, clusterId) in topCluster)
        {
            if (nodeById.TryGetValue(nodeId, out var node))
            {
                if (!grouped.TryGetValue(clusterId, out var list))
                {
                    list = [];
                    grouped[clusterId] = list;
                }

                list.Add(node);
            }
        }

        foreach (var (clusterId, list) in grouped)
        {
            var minX = double.PositiveInfinity;
            var maxX = double.NegativeInfinity;
            var minY = double.PositiveInfinity;
            var maxY = double.NegativeInfinity;
            foreach (var node in list)
            {
                minX = Math.Min(minX, node.Position.X - node.Width / 2);
                maxX = Math.Max(maxX, node.Position.X + node.Width / 2);
                minY = Math.Min(minY, node.Position.Y - node.Height / 2);
                maxY = Math.Max(maxY, node.Position.Y + node.Height / 2);
            }

            clusterBox[clusterId] = new(
                minX - clusterPadding - routeMargin,
                minY - clusterPadding - clusterLabelHeight - routeMargin,
                maxX + clusterPadding + routeMargin,
                maxY + clusterPadding + routeMargin);
        }

        var obstacles = new List<BoxRouter.Box>(clusterBox.Count);
        foreach (var edge in diagram.Edges)
        {
            var source = topCluster.GetValueOrDefault(edge.SourceId);
            var target = topCluster.GetValueOrDefault(edge.TargetId);

            // Intra-cluster edges keep their isolated routing; apply the cluster's total shift to its points.
            if (source is not null && source == target)
            {
                var (dx, dy) = offset.GetValueOrDefault(source);
                for (var i = 0; i < edge.Points.Count; i++)
                {
                    edge.Points[i] = new(edge.Points[i].X + dx, edge.Points[i].Y + dy);
                }

                continue;
            }

            if (!nodeById.TryGetValue(edge.SourceId, out var s) ||
                !nodeById.TryGetValue(edge.TargetId, out var t))
            {
                continue;
            }

            // Route around every cluster box except the two the edge connects.
            obstacles.Clear();
            foreach (var (clusterId, box) in clusterBox)
            {
                if (clusterId != source && clusterId != target)
                {
                    obstacles.Add(box);
                }
            }

            var path = BoxRouter.Route(s.Position, t.Position, obstacles);
            path[0] = BorderPoint(s, path[1]);
            path[^1] = BorderPoint(t, path[^2]);

            edge.Points.Clear();
            edge.Points.AddRange(path);
        }
    }

    /// <summary>Pushes overlapping top-level cluster boxes apart horizontally (accumulating the shift into
    /// each cluster's offset), guaranteeing the rendered boxes don't overlap.</summary>
    static void SeparateClusterBoxes(
        GraphDiagramBase diagram,
        Dictionary<string, Node> nodeById,
        Dictionary<string, string> topCluster,
        Dictionary<string, (double Dx, double Dy)> offset,
        double gap)
    {
        var members = new Dictionary<string, List<Node>>(StringComparer.Ordinal);
        foreach (var (nodeId, clusterId) in topCluster)
        {
            if (nodeById.TryGetValue(nodeId, out var node))
            {
                if (!members.TryGetValue(clusterId, out var list))
                {
                    list = [];
                    members[clusterId] = list;
                }

                list.Add(node);
            }
        }

        var ids = members.Keys.ToList();

        for (var iteration = 0; iteration < 16; iteration++)
        {
            var box = new Dictionary<string, (double MinX, double MaxX, double MinY, double MaxY)>(StringComparer.Ordinal);
            foreach (var (clusterId, list) in members)
            {
                var minX = double.PositiveInfinity;
                var maxX = double.NegativeInfinity;
                var minY = double.PositiveInfinity;
                var maxY = double.NegativeInfinity;
                foreach (var node in list)
                {
                    minX = Math.Min(minX, node.Position.X - node.Width / 2);
                    maxX = Math.Max(maxX, node.Position.X + node.Width / 2);
                    minY = Math.Min(minY, node.Position.Y - node.Height / 2);
                    maxY = Math.Max(maxY, node.Position.Y + node.Height / 2);
                }

                box[clusterId] = (
                    minX - clusterPadding,
                    maxX + clusterPadding,
                    minY - clusterPadding - clusterLabelHeight,
                    maxY + clusterPadding);
            }

            ids.Sort((a, b) => box[a].MinX.CompareTo(box[b].MinX));

            var changed = false;
            for (var i = 0; i < ids.Count; i++)
            {
                for (var j = i + 1; j < ids.Count; j++)
                {
                    var a = box[ids[i]];
                    var b = box[ids[j]];
                    if (a.MaxY <= b.MinY || b.MaxY <= a.MinY)
                    {
                        continue;
                    }

                    var shift = a.MaxX + gap - b.MinX;
                    if (shift <= 0)
                    {
                        continue;
                    }

                    foreach (var node in members[ids[j]])
                    {
                        node.Position = new(node.Position.X + shift, node.Position.Y);
                    }

                    var current = offset.GetValueOrDefault(ids[j]);
                    offset[ids[j]] = (current.Dx + shift, current.Dy);
                    box[ids[j]] = b with { MinX = b.MinX + shift, MaxX = b.MaxX + shift };
                    changed = true;
                }
            }

            if (!changed)
            {
                break;
            }
        }
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

    sealed class ClusterGraph : GraphDiagramBase;
}
