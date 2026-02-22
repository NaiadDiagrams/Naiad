namespace MermaidSharp.Layout;

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
        AssignPositionCoordinates(graph, nodeSep, isHorizontal);

        // Handle direction reversal
        AdjustForDirection(graph, direction);
    }

    static void AssignRankCoordinates(LayoutGraph graph, double rankSep, bool isHorizontal)
    {
        double currentY = 0;

        for (var r = 0; r < graph.Ranks.Length; r++)
        {
            var nodesInRank = graph.Ranks[r];
            var maxHeight = nodesInRank.Count > 0
                ? nodesInRank.Max(_ => isHorizontal ? _.Width : _.Height)
                : 0;

            foreach (var node in nodesInRank)
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

    static void AssignPositionCoordinates(LayoutGraph graph, double nodeSep, bool isHorizontal)
    {
        // Use block positioning with median alignment
        // This is a simplified version of Brandes-Köpf

        // Pass 1: Position nodes left-aligned within ranks
        for (var r = 0; r < graph.Ranks.Length; r++)
        {
            var nodesInRank = graph.Ranks[r].OrderBy(_ => _.Order).ToList();
            double currentX = 0;

            foreach (var node in nodesInRank)
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
        for (var iteration = 0; iteration < 4; iteration++)
        {
            // Down pass
            for (var r = 1; r < graph.Ranks.Length; r++)
            {
                AlignToNeighbors(graph, r, true, nodeSep, isHorizontal);
            }

            // Up pass
            for (var r = graph.Ranks.Length - 2; r >= 0; r--)
            {
                AlignToNeighbors(graph, r, false, nodeSep, isHorizontal);
            }
        }

        // Normalize positions to start at 0
        NormalizePositions(graph);
    }

    static void AlignToNeighbors(LayoutGraph graph, int rank, bool useInEdges,
        double nodeSep, bool isHorizontal)
    {
        var nodesInRank = graph.Ranks[rank].OrderBy(_ => _.Order).ToList();

        foreach (var node in nodesInRank)
        {
            var neighbors = useInEdges
                ? graph.GetPredecessors(node.Id).ToList()
                : graph.GetSuccessors(node.Id).ToList();

            if (neighbors.Count == 0)
            {
                continue;
            }

            // Calculate median position of neighbors
            var positions = neighbors
                .Select(_ => isHorizontal ? _.Y : _.X)
                .OrderBy(_ => _)
                .ToList();

            var targetPos = Median(positions);
            var currentPos = isHorizontal ? node.Y : node.X;

            // Only move if it improves alignment and doesn't cause overlap
            var delta = targetPos - currentPos;
            if (Math.Abs(delta) > 0.1)
            {
                var canMove = CanMoveNode(graph, node, delta, nodeSep, isHorizontal);
                if (canMove)
                {
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
    }

    static bool CanMoveNode(LayoutGraph graph, LayoutNode node, double delta,
        double nodeSep, bool isHorizontal)
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

    static void NormalizePositions(LayoutGraph graph)
    {
        if (graph.Nodes.Count == 0)
        {
            return;
        }

        var minX = graph.Nodes.Values.Min(_ => _.X - _.Width / 2);
        var minY = graph.Nodes.Values.Min(_ => _.Y - _.Height / 2);

        foreach (var node in graph.Nodes.Values)
        {
            node.X -= minX;
            node.Y -= minY;
        }
    }

    static void AdjustForDirection(LayoutGraph graph, Direction direction)
    {
        if (graph.Nodes.Count == 0)
        {
            return;
        }

        switch (direction)
        {
            case Direction.BottomToTop:
                var maxY = graph.Nodes.Values.Max(_ => _.Y);
                foreach (var node in graph.Nodes.Values)
                {
                    node.Y = maxY - node.Y;
                }
                break;

            case Direction.RightToLeft:
                var maxX = graph.Nodes.Values.Max(_ => _.X);
                foreach (var node in graph.Nodes.Values)
                {
                    node.X = maxX - node.X;
                }
                break;
        }
    }

    // Arrow marker size - the arrowhead extends this far past the line endpoint
    const double ArrowMarkerOffset = 5;

    public static void RouteEdges(LayoutGraph graph, Direction direction)
    {
        var isHorizontal = direction is Direction.LeftToRight or Direction.RightToLeft;

        foreach (var edge in graph.Edges)
        {
            var source = graph.GetNode(edge.SourceId);
            var target = graph.GetNode(edge.TargetId);

            if (source is null || target is null)
            {
                continue;
            }

            edge.Points.Clear();

            if (source.IsDummy || target.IsDummy)
            {
                // Part of a long edge - just add the node positions
                edge.Points.Add(new(source.X, source.Y));
                edge.Points.Add(new(target.X, target.Y));
            }
            else
            {
                // Regular edge - create path through dummy nodes if any
                // For horizontal layout: connect right edge of source
                // For vertical layout: connect bottom edge of source
                var sourceEdgeX = isHorizontal ? source.X + source.Width / 2 : source.X;
                var sourceEdgeY = isHorizontal ? source.Y : source.Y + source.Height / 2;
                edge.Points.Add(new(sourceEdgeX, sourceEdgeY));

                // Find dummy nodes for this edge
                var dummies = graph.Nodes.Values
                    .Where(_ => _.IsDummy &&
                                _.OriginalEdgeSource == edge.SourceId &&
                                _.OriginalEdgeTarget == edge.TargetId)
                    .OrderBy(_ => _.Rank)
                    .ToList();

                foreach (var dummy in dummies)
                {
                    edge.Points.Add(new(dummy.X, dummy.Y));
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

                if (length > ArrowMarkerOffset)
                {
                    // Shorten the endpoint by the arrow marker size
                    var ratio = (length - ArrowMarkerOffset) / length;
                    targetEdgeX = lastPoint.X + dx * ratio;
                    targetEdgeY = lastPoint.Y + dy * ratio;
                }

                edge.Points.Add(new(targetEdgeX, targetEdgeY));
            }
        }
    }
}
