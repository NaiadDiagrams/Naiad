namespace Naiad.Dagre;

/// <summary>Faithful port of dagre's <c>rank/network-simplex.ts</c>.</summary>
static class NetworkSimplex
{
    /*
     * The network simplex algorithm assigns ranks to each node in the input graph
     * and iteratively improves the ranking to reduce the length of edges.
     *
     * Preconditions:
     *
     *    1. The input graph must be a DAG.
     *    2. All nodes in the graph must have an object value.
     *    3. All edges in the graph must have "minlen" and "weight" attributes.
     *
     * Postconditions:
     *
     *    1. All nodes in the graph will have an assigned "rank" attribute that has
     *       been optimized by the network simplex algorithm. Ranks start at 0.
     */
    public static void Run(Graph graph)
    {
        graph = Util.Simplify(graph);
        RankUtil.LongestPath(graph);

        var t = FeasibleTree.Run(graph);
        InitLowLimValues(t);
        InitCutValues(t, graph);

        Edge? e;
        while ((e = LeaveEdge(t)) != null)
        {
            var f = EnterEdge(t, graph, e);
            ExchangeEdges(t, graph, e, f);
        }
    }

    /*
     * Initializes cut values for all edges in the tree.
     */
    public static void InitCutValues(Graph tree, Graph graph)
    {
        var visitedNodes = Alg.Postorder(tree, tree.Nodes());
        visitedNodes = visitedNodes.GetRange(0, visitedNodes.Count - 1);
        foreach (var v in visitedNodes)
        {
            AssignCutValue(tree, graph, v);
        }
    }

    public static void AssignCutValue(Graph tree, Graph graph, string child)
    {
        var childLab = tree.Node(child);
        var parent = childLab.Parent!;
        var edge = tree.Edge_(child, parent);
        edge.Cutvalue = CalcCutValue(tree, graph, child);
    }

    /*
     * Given the tight tree, its graph, and a child in the graph calculate and
     * return the cut value for the edge between the child and its parent.
     */
    public static double CalcCutValue(Graph tree, Graph graph, string child)
    {
        var childLab = tree.Node(child);
        var parent = childLab.Parent!;
        // True if the child is on the tail end of the edge in the directed graph
        var childIsTail = true;
        // The graph's view of the tree edge we're inspecting
        var graphEdge = graph.Edge_(child, parent);
        // The accumulated cut value for the edge between this node and its parent
        double cutValue;

        if (graphEdge == null)
        {
            childIsTail = false;
            graphEdge = graph.Edge_(parent, child);
        }

        cutValue = graphEdge.Weight!.Value;

        var nodeEdges = graph.NodeEdges(child);
        if (nodeEdges != null)
        {
            foreach (var edge in nodeEdges)
            {
                var isOutEdge = edge.V == child;
                var other = isOutEdge ? edge.W : edge.V;

                if (other != parent)
                {
                    var pointsToHead = isOutEdge == childIsTail;
                    var otherWeight = graph.Edge_(edge).Weight!.Value;

                    cutValue += pointsToHead ? otherWeight : -otherWeight;
                    if (IsTreeEdge(tree, child, other))
                    {
                        var treeEdge = tree.Edge_(child, other);
                        var otherCutValue = treeEdge.Cutvalue!.Value;
                        cutValue += pointsToHead ? -otherCutValue : otherCutValue;
                    }
                }
            }
        }

        return cutValue;
    }

    public static void InitLowLimValues(Graph tree) =>
        InitLowLimValues(tree, tree.Nodes()[0]);

    public static void InitLowLimValues(Graph tree, string root) =>
        DfsAssignLowLim(tree, new Dictionary<string, bool>(StringComparer.Ordinal), 1, root, null);

    public static int DfsAssignLowLim(Graph tree, Dictionary<string, bool> visited, int nextLim, string v, string? parent)
    {
        var low = nextLim;
        var label = tree.Node(v);

        visited[v] = true;
        var neighbors = tree.Neighbors(v);
        if (neighbors != null)
        {
            foreach (var w in neighbors)
            {
                if (!visited.ContainsKey(w))
                {
                    nextLim = DfsAssignLowLim(tree, visited, nextLim, w, v);
                }
            }
        }

        label.Low = low;
        label.Lim = nextLim++;
        if (parent != null)
        {
            label.Parent = parent;
        }
        else
        {
            // TODO should be able to remove this when we incrementally update low lim
            label.Parent = null;
        }

        return nextLim;
    }

    public static Edge? LeaveEdge(Graph tree) =>
        tree.Edges().FirstOrDefault(e =>
        {
            var edge = tree.Edge_(e);
            return edge.Cutvalue!.Value < 0;
        });

    public static Edge EnterEdge(Graph tree, Graph graph, Edge edge)
    {
        var v = edge.V;
        var w = edge.W;

        // For the rest of this function we assume that v is the tail and w is the
        // head, so if we don't have this edge in the graph we should flip it to
        // match the correct orientation.
        if (!graph.HasEdge(v, w))
        {
            v = edge.W;
            w = edge.V;
        }

        var vLabel = tree.Node(v);
        var wLabel = tree.Node(w);
        var tailLabel = vLabel;
        var flip = false;

        // If the root is in the tail of the edge then we need to flip the logic that
        // checks for the head and tail nodes in the candidates function below.
        if (vLabel.Lim!.Value > wLabel.Lim!.Value)
        {
            tailLabel = wLabel;
            flip = true;
        }

        var candidates = graph.Edges().Where(candidate =>
            flip == IsDescendant(tree, tree.Node(candidate.V), tailLabel) &&
            flip != IsDescendant(tree, tree.Node(candidate.W), tailLabel)).ToList();

        var acc = candidates[0];
        for (var i = 1; i < candidates.Count; i++)
        {
            var candidate = candidates[i];
            if (RankUtil.Slack(graph, candidate) < RankUtil.Slack(graph, acc))
            {
                acc = candidate;
            }
        }

        return acc;
    }

    public static void ExchangeEdges(Graph t, Graph g, Edge e, Edge f)
    {
        var v = e.V;
        var w = e.W;
        t.RemoveEdge(v, w);
        t.SetEdge(f.V, f.W, new EdgeLabel());
        InitLowLimValues(t);
        InitCutValues(t, g);
        UpdateRanks(t, g);
    }

    public static void UpdateRanks(Graph t, Graph g)
    {
        var root = t.Nodes().FirstOrDefault(v =>
        {
            var node = t.Node(v);
            return node.Parent == null;
        });
        if (root == null)
        {
            return;
        }

        var vs = Alg.Preorder(t, [root]);
        vs = vs.GetRange(1, vs.Count - 1);
        foreach (var v in vs)
        {
            var treeNode = t.Node(v);
            var parent = treeNode.Parent!;
            var edge = g.Edge_(v, parent);
            var flipped = false;

            if (edge == null)
            {
                edge = g.Edge_(parent, v);
                flipped = true;
            }

            g.Node(v).Rank = g.Node(parent).Rank!.Value + (flipped ? edge.Minlen!.Value : -edge.Minlen!.Value);
        }
    }

    /*
     * Returns true if the edge is in the tree.
     */
    public static bool IsTreeEdge(Graph tree, string u, string v) =>
        tree.HasEdge(u, v);

    /*
     * Returns true if the specified node is descendant of the root node per the
     * assigned low and lim attributes in the tree.
     */
    public static bool IsDescendant(Graph tree, NodeLabel vLabel, NodeLabel rootLabel) =>
        rootLabel.Low!.Value <= vLabel.Lim!.Value && vLabel.Lim!.Value <= rootLabel.Lim!.Value;
}
