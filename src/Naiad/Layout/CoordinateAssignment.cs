static class CoordinateAssignment
{
    public static void Run(LayoutGraph graph, double nodeSep, double rankSep, Direction direction)
    {
        graph.BuildRanks();
        graph.UpdateOrderInRanks();

        var isHorizontal = direction is Direction.LeftToRight or Direction.RightToLeft;

        // Assign Y coordinates based on ranks
        AssignRankCoordinates(graph, rankSep, isHorizontal);

        // Assign X coordinates using simplified Brandes-Köpf
        var (maxX, maxY) = AssignPositionCoordinates(graph, nodeSep, isHorizontal);

        // Handle direction reversal
        AdjustForDirection(graph, direction, maxX, maxY);
    }

    static void AssignRankCoordinates(LayoutGraph graph, double rankSep, bool isHorizontal)
    {
        double currentY = 0;

        foreach (var rank in graph.Ranks)
        {
            var maxHeight = rank.Count > 0
                ? rank.Max(_ => isHorizontal ? _.Width : _.Height)
                : 0;

            foreach (var node in rank)
            {
                if (isHorizontal)
                {
                    node.X = currentY + maxHeight / 2;
                }
                else
                {
                    node.Y = currentY + maxHeight / 2;
                }
            }

            currentY += maxHeight + rankSep;
        }
    }

    static (double maxX, double maxY) AssignPositionCoordinates(LayoutGraph graph, double nodeSep, bool isHorizontal)
    {
        // Use block positioning with median alignment
        // This is a simplified version of Brandes-Köpf

        // Pass 1: Position nodes left-aligned within ranks
        foreach (var rank in graph.Ranks)
        {
            rank.Sort((a, b) => a.Order.CompareTo(b.Order));
            double currentX = 0;

            foreach (var node in rank)
            {
                var nodeWidth = isHorizontal ? node.Height : node.Width;
                if (isHorizontal)
                {
                    node.Y = currentX + nodeWidth / 2;
                }
                else
                {
                    node.X = currentX + nodeWidth / 2;
                }

                currentX += nodeWidth + nodeSep;
            }
        }

        // Pass 2: Center alignment based on connected nodes
        var positions = new List<double>();
        for (var iteration = 0; iteration < 4; iteration++)
        {
            // Down pass
            for (var index = 1; index < graph.Ranks.Length; index++)
            {
                AlignToNeighbors(graph, index, true, nodeSep, isHorizontal, positions);
            }

            // Up pass
            for (var r = graph.Ranks.Length - 2; r >= 0; r--)
            {
                AlignToNeighbors(graph, r, false, nodeSep, isHorizontal, positions);
            }
        }

        // Normalize positions to start at 0
        return NormalizePositions(graph);
    }

    static void AlignToNeighbors(LayoutGraph graph, int rank, bool useInEdges,
        double nodeSep, bool isHorizontal, List<double> positions)
    {
        var nodesInRank = graph.Ranks[rank];
        nodesInRank.Sort((a, b) => a.Order.CompareTo(b.Order));

        foreach (var node in nodesInRank)
        {
            positions.Clear();
            if (useInEdges)
            {
                foreach (var edge in node.InEdges)
                {
                    if (edge.Source is { } source)
                    {
                        positions.Add(isHorizontal ? source.Y : source.X);
                    }
                }
            }
            else
            {
                foreach (var edge in node.OutEdges)
                {
                    if (edge.Target is { } target)
                    {
                        positions.Add(isHorizontal ? target.Y : target.X);
                    }
                }
            }

            if (positions.Count == 0)
            {
                continue;
            }

            positions.Sort();

            var targetPos = Median(positions);
            var currentPos = isHorizontal ? node.Y : node.X;

            // Only move if it improves alignment and doesn't cause overlap
            var delta = targetPos - currentPos;
            if (Math.Abs(delta) > 0.1)
            {
                var canMove = CanMoveNode(graph, node, delta, nodeSep, isHorizontal);
                if (!canMove)
                {
                    continue;
                }

                if (isHorizontal)
                {
                    node.Y = targetPos;
                }
                else
                {
                    node.X = targetPos;
                }
            }
        }
    }

    static bool CanMoveNode(
        LayoutGraph graph,
        LayoutNode node,
        double delta,
        double nodeSep,
        bool isHorizontal)
    {
        var nodesInRank = graph.Ranks[node.Rank];
        var nodePos = isHorizontal ? node.Y : node.X;
        var newPos = nodePos + delta;
        var nodeSize = isHorizontal ? node.Height : node.Width;

        foreach (var other in nodesInRank)
        {
            if (other.Id == node.Id)
            {
                continue;
            }

            var otherPos = isHorizontal ? other.Y : other.X;
            var otherSize = isHorizontal ? other.Height : other.Width;
            var minDist = (nodeSize + otherSize) / 2 + nodeSep;

            if (Math.Abs(newPos - otherPos) < minDist)
            {
                return false;
            }
        }

        return true;
    }

    static double Median(List<double> values)
    {
        if (values.Count == 0)
        {
            return 0;
        }

        if (values.Count == 1)
        {
            return values[0];
        }

        var mid = values.Count / 2;
        if (values.Count % 2 == 0)
        {
            return (values[mid - 1] + values[mid]) / 2;
        }

        return values[mid];
    }

    static (double maxX, double maxY) NormalizePositions(LayoutGraph graph)
    {
        if (graph.Nodes.Count == 0)
        {
            return (0, 0);
        }

        var minX = double.PositiveInfinity;
        var minY = double.PositiveInfinity;
        foreach (var node in graph.Nodes.Values)
        {
            var left = node.X - node.Width / 2;
            var top = node.Y - node.Height / 2;
            if (left < minX)
            {
                minX = left;
            }

            if (top < minY)
            {
                minY = top;
            }
        }

        var maxX = double.NegativeInfinity;
        var maxY = double.NegativeInfinity;
        foreach (var node in graph.Nodes.Values)
        {
            node.X -= minX;
            node.Y -= minY;
            if (node.X > maxX)
            {
                maxX = node.X;
            }

            if (node.Y > maxY)
            {
                maxY = node.Y;
            }
        }

        return (maxX, maxY);
    }

    static void AdjustForDirection(LayoutGraph graph, Direction direction, double maxX, double maxY)
    {
        switch (direction)
        {
            case Direction.BottomToTop:
                foreach (var node in graph.Nodes.Values)
                {
                    node.Y = maxY - node.Y;
                }

                break;

            case Direction.RightToLeft:
                foreach (var node in graph.Nodes.Values)
                {
                    node.X = maxX - node.X;
                }

                break;
        }
    }

    // Arrow marker size - the arrowhead extends this far past the line endpoint
    const double arrowMarkerOffset = 5;

    public static void RouteEdges(LayoutGraph graph, Direction direction)
    {
        var isHorizontal = direction is Direction.LeftToRight or Direction.RightToLeft;

        // Build lookup for dummy nodes by (OriginalEdgeSource, OriginalEdgeTarget)
        var dummyLookup = new Dictionary<(string, string), List<LayoutNode>>();
        foreach (var node in graph.Nodes.Values)
        {
            if (node is not
                {
                    IsDummy: true,
                    OriginalEdgeSource: not null,
                    OriginalEdgeTarget: not null
                })
            {
                continue;
            }

            var key = (node.OriginalEdgeSource, node.OriginalEdgeTarget);
            if (dummyLookup.TryGetValue(key, out var list))
            {
                list.Add(node);
            }
            else
            {
                dummyLookup[key] = [node];
            }
        }

        // Sort each dummy list by rank once
        foreach (var list in dummyLookup.Values)
        {
            list.Sort((a, b) => a.Rank.CompareTo(b.Rank));
        }

        foreach (var edge in graph.Edges)
        {
            var source = graph.GetNode(edge.SourceId);
            var target = graph.GetNode(edge.TargetId);

            if (source is null ||
                target is null)
            {
                continue;
            }

            edge.Points.Clear();

            if (source.IsDummy || target.IsDummy)
            {
                // Part of a long edge - just add the node positions
                edge.Points.Add(new(source.X, source.Y));
                edge.Points.Add(new(target.X, target.Y));
                continue;
            }

            // Regular edge - create path through dummy nodes if any
            var sourceEdgeX = isHorizontal ? source.X + source.Width / 2 : source.X;
            var sourceEdgeY = isHorizontal ? source.Y : source.Y + source.Height / 2;
            edge.Points.Add(new(sourceEdgeX, sourceEdgeY));

            // Find dummy nodes for this edge using pre-built lookup
            if (dummyLookup.TryGetValue((edge.SourceId, edge.TargetId), out var dummies))
            {
                foreach (var dummy in dummies)
                {
                    edge.Points.Add(new(dummy.X, dummy.Y));
                }
            }

            // Calculate the target endpoint, offset to account for arrow marker
            // For horizontal layout: connect left edge of target
            // For vertical layout: connect top edge of target
            var targetEdgeX = isHorizontal ? target.X - target.Width / 2 : target.X;
            var targetEdgeY = isHorizontal ? target.Y : target.Y - target.Height / 2;

            // Get the last point before target to determine edge direction
            var lastPoint = edge.Points[^1];
            var dx = targetEdgeX - lastPoint.X;
            var dy = targetEdgeY - lastPoint.Y;
            var length = Math.Sqrt(dx * dx + dy * dy);

            if (length > arrowMarkerOffset)
            {
                // Shorten the endpoint by the arrow marker size
                var ratio = (length - arrowMarkerOffset) / length;
                targetEdgeX = lastPoint.X + dx * ratio;
                targetEdgeY = lastPoint.Y + dy * ratio;
            }

            edge.Points.Add(new(targetEdgeX, targetEdgeY));
        }
    }
}
