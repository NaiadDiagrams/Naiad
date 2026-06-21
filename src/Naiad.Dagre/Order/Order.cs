namespace Naiad.Dagre;

/// <summary>Options for <see cref="Order.Run"/>. Faithful to the TS <c>OrderOptions</c> interface.
/// (<c>OrderConstraint</c> is defined alongside <see cref="LayoutOptions"/> in Layout.cs.)</summary>
sealed class OrderOptions
{
    public Action<Graph, Action<Graph, OrderOptions>>? CustomOrder;
    public bool? DisableOptimalOrderHeuristic;
    public List<OrderConstraint>? Constraints;
}

/// <summary>Faithful port of dagre's <c>order/index.ts</c>.</summary>
static class Order
{
    public static void Run(Graph graph, OrderOptions? opts = null)
    {
        opts ??= new OrderOptions();

        if (opts.CustomOrder != null)
        {
            opts.CustomOrder(graph, Run);
            return;
        }

        var maxRank = (int) Util.MaxRank(graph);
        var downLayerGraphs = BuildLayerGraphs(graph, Util.Range(1, maxRank + 1), "inEdges");
        var upLayerGraphs = BuildLayerGraphs(graph, Util.Range(maxRank - 1, -1, -1), "outEdges");

        var layering = InitOrder.Run(graph);
        AssignOrder(graph, layering);

        if (opts.DisableOptimalOrderHeuristic == true)
        {
            return;
        }

        var bestCC = double.PositiveInfinity;
        List<List<string>>? best = null;

        var constraints = opts.Constraints ?? [];
        for (int i = 0, lastBest = 0; lastBest < 4; ++i, ++lastBest)
        {
            SweepLayerGraphs(i % 2 != 0 ? downLayerGraphs : upLayerGraphs, i % 4 >= 2, constraints);

            layering = Util.BuildLayerMatrix(graph);
            var cc = CrossCount.Run(graph, layering);
            if (cc < bestCC)
            {
                lastBest = 0;
                // Object.assign({}, layering): shallow copy (inner layer references shared).
                best = new List<List<string>>(layering);
                bestCC = cc;
            }
            else if (cc == bestCC)
            {
                // structuredClone(layering): deep clone.
                best = layering.Select(layer => new List<string>(layer)).ToList();
            }
        }

        AssignOrder(graph, best!);
    }

    static List<Graph> BuildLayerGraphs(Graph graph, List<int> ranks, string relationship)
    {
        // Build an index mapping from rank to the nodes with that rank.
        // This helps to avoid a quadratic search for all nodes with the same rank as
        // the current node.
        var nodesByRank = new Dictionary<int, List<string>>();

        void AddNodeToRank(int rank, string node)
        {
            if (!nodesByRank.TryGetValue(rank, out var list))
            {
                list = [];
                nodesByRank[rank] = list;
            }

            list.Add(node);
        }

        // Visit the nodes in their original order in the graph, and add each
        // node to the ranks(s) that it belongs to.
        foreach (var v in graph.Nodes())
        {
            var node = graph.Node(v);
            if (node.Rank != null)
            {
                AddNodeToRank(node.Rank.Value, v);
            }

            // If there is a range of ranks, add it to each, but skip the `node.rank` which
            // has already had the node added.
            if (node.MinRank != null && node.MaxRank != null)
            {
                for (var r = node.MinRank.Value; r <= node.MaxRank.Value; r++)
                {
                    if (r != node.Rank)
                    {
                        // Don't add this node to its `node.rank` twice.
                        AddNodeToRank(r, v);
                    }
                }
            }
        }

        return ranks.Select(rank =>
            BuildLayerGraph.Run(graph, rank, relationship, nodesByRank.GetValueOrDefault(rank) ?? [])).ToList();
    }

    static void SweepLayerGraphs(List<Graph> layerGraphs, bool biasRight, List<OrderConstraint> constraints)
    {
        var cg = new Graph();
        foreach (var lg in layerGraphs)
        {
            foreach (var con in constraints)
            {
                cg.SetEdge(con.Left, con.Right);
            }

            var root = lg.Graph_().Root!;
            var sorted = SortSubgraph.Run(lg, root, cg, biasRight);
            for (var i = 0; i < sorted.Vs.Count; i++)
            {
                lg.Node(sorted.Vs[i]).Order = i;
            }

            AddSubgraphConstraints.Run(lg, cg, sorted.Vs);
        }
    }

    static void AssignOrder(Graph graph, List<List<string>> layering)
    {
        foreach (var layer in layering)
        {
            for (var i = 0; i < layer.Count; i++)
            {
                graph.Node(layer[i]).Order = i;
            }
        }
    }
}
