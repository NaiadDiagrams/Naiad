namespace MermaidSharp;

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

        for (int r = 0; r < graph.Ranks.Length; r++)
        {
            var nodesInRank = graph.Ranks[r];
            var maxHeight = nodesInRank.Count > 0
                ? nodesInRank.Max(n => isHorizontal ? n.Width : n.Height)
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
        for (int r = 0; r < graph.Ranks.Length; r++)
        {
            var nodesInRank = graph.Ranks[r].OrderBy(n => n.Order).ToList();
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
        for (int iteration = 0; iteration < 4; iteration++)
        {
            // Down pass
            for (int r = 1; r < graph.Ranks.Length; r++)
            {
                AlignToNeighbors(graph, r, true, nodeSep, isHorizontal);
            }

            // Up pass
            for (int r = graph.Ranks.Length - 2; r >= 0; r--)
            {
                AlignToNeighbors(graph, r, false, nodeSep, isHorizontal);
            }
        }

        // Normalize positions to start at 0
        NormalizePositions(graph, isHorizontal);
    }

    static void AlignToNeighbors(LayoutGraph graph, int rank, bool useInEdges,
        double nodeSep, bool isHorizontal)
    {
        var nodesInRank = graph.Ranks[rank].OrderBy(n => n.Order).ToList();

        foreach (var node in nodesInRank)
        {
            var neighbors = useInEdges
                ? graph.GetPredecessors(node.Id).ToList()
                : graph.GetSuccessors(node.Id).ToList();

            if (neighbors.Count == 0) continue;

            // Calculate median position of neighbors
            var positions = neighbors
                .Select(n => isHorizontal ? n.Y : n.X)
                .OrderBy(x => x)
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
            if (other.Id == node.Id) continue;

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
        if (values.Count == 0) return 0;
        if (values.Count == 1) return values[0];

        var mid = values.Count / 2;
        return values.Count % 2 == 0
            ? (values[mid - 1] + values[mid]) / 2
            : values[mid];
    }

    static void NormalizePositions(LayoutGraph graph, bool isHorizontal)
    {
        if (graph.Nodes.Count == 0) return;

        var minX = graph.Nodes.Values.Min(n => n.X - n.Width / 2);
        var minY = graph.Nodes.Values.Min(n => n.Y - n.Height / 2);

        foreach (var node in graph.Nodes.Values)
        {
            node.X -= minX;
            node.Y -= minY;
        }
    }

    static void AdjustForDirection(LayoutGraph graph, Direction direction)
    {
        if (graph.Nodes.Count == 0) return;

        switch (direction)
        {
            case Direction.BottomToTop:
                var maxY = graph.Nodes.Values.Max(n => n.Y);
                foreach (var node in graph.Nodes.Values)
                {
                    node.Y = maxY - node.Y;
                }
                break;

            case Direction.RightToLeft:
                var maxX = graph.Nodes.Values.Max(n => n.X);
                foreach (var node in graph.Nodes.Values)
                {
                    node.X = maxX - node.X;
                }
                break;
        }
    }

    public static void RouteEdges(LayoutGraph graph)
    {
        foreach (var edge in graph.Edges)
        {
            var source = graph.GetNode(edge.SourceId);
            var target = graph.GetNode(edge.TargetId);

            if (source is null || target is null) continue;

            edge.Points.Clear();

            if (source.IsDummy || target.IsDummy)
            {
                // Part of a long edge - just add the node positions
                edge.Points.Add(new Position(source.X, source.Y));
                edge.Points.Add(new Position(target.X, target.Y));
            }
            else
            {
                // Regular edge - create path through dummy nodes if any
                edge.Points.Add(new Position(source.X, source.Y + source.Height / 2));

                // Find dummy nodes for this edge
                var dummies = graph.Nodes.Values
                    .Where(n => n.IsDummy &&
                                n.OriginalEdgeSource == edge.SourceId &&
                                n.OriginalEdgeTarget == edge.TargetId)
                    .OrderBy(n => n.Rank)
                    .ToList();

                foreach (var dummy in dummies)
                {
                    edge.Points.Add(new Position(dummy.X, dummy.Y));
                }

                edge.Points.Add(new Position(target.X, target.Y - target.Height / 2));
            }
        }
    }
}
