namespace Naiad.Dagre;

/*
 * A nesting graph creates dummy nodes for the tops and bottoms of subgraphs,
 * adds appropriate edges to ensure that all cluster nodes are placed between
 * these boundaries, and ensures that the graph is connected.
 *
 * In addition we ensure, through the use of the minlen property, that nodes
 * and subgraph border nodes to not end up on the same rank.
 *
 * Preconditions:
 *
 *    1. Input graph is a DAG
 *    2. Nodes in the input graph has a minlen attribute
 *
 * Postconditions:
 *
 *    1. Input graph is connected.
 *    2. Dummy nodes are added for the tops and bottoms of subgraphs.
 *    3. The minlen attribute for nodes is adjusted to ensure nodes do not
 *       get placed on the same rank as subgraph border nodes.
 *
 * The nesting graph idea comes from Sander, "Layout of Compound Directed
 * Graphs."
 */
static class NestingGraph
{
    public static void Run(Graph graph)
    {
        var root = Util.AddDummyNode(graph, "root", new NodeLabel(), "_root");
        var depths = TreeDepths(graph);
        var depthsArr = depths.Values.Select(d => (double) d).ToList();
        var height = Util.ApplyMax(depthsArr) - 1; // Note: depths is an Object not an array
        var nodeSep = 2 * height + 1;

        graph.Graph_().NestingRoot = root;

        // Multiply minlen by nodeSep to align nodes on non-border ranks.
        // The real pipeline guarantees every edge has a minlen before this runs; the unit
        // tests can leave it unset, mirroring JS where `undefined *= nodeSep` is NaN-but-unused.
        foreach (var e in graph.Edges())
        {
            var edge = graph.Edge_(e);
            if (edge.Minlen.HasValue)
            {
                edge.Minlen = (int) (edge.Minlen.Value * nodeSep);
            }
        }

        // Calculate a weight that is sufficient to keep subgraphs vertically compact
        var weight = SumWeights(graph) + 1;

        // Create border nodes and link them up
        foreach (var child in graph.Children(Util.GraphNode))
        {
            Dfs(graph, root, nodeSep, weight, height, depths, child);
        }

        // Save the multiplier for node layers for later removal of empty border
        // layers.
        graph.Graph_().NodeRankFactor = (int) nodeSep;
    }

    static void Dfs(
        Graph graph,
        string root,
        double nodeSep,
        double weight,
        double height,
        Dictionary<string, int> depths,
        string v)
    {
        var children = graph.Children(v);
        if (children.Count == 0)
        {
            if (v != root)
            {
                graph.SetEdge(root, v, new EdgeLabel { Weight = 0, Minlen = (int) nodeSep });
            }

            return;
        }

        var top = Util.AddBorderNode(graph, "_bt");
        var bottom = Util.AddBorderNode(graph, "_bb");
        var label = graph.Node(v);

        graph.SetParent(top, v);
        label.BorderTop = top;
        graph.SetParent(bottom, v);
        label.BorderBottom = bottom;

        foreach (var child in children)
        {
            Dfs(graph, root, nodeSep, weight, height, depths, child);

            var childNode = graph.Node(child);
            var childTop = childNode.BorderTop ?? child;
            var childBottom = childNode.BorderBottom ?? child;
            var thisWeight = childNode.BorderTop != null ? weight : 2 * weight;
            var minlen = childTop != childBottom ? 1 : height - (depths.GetValueOrDefault(v, 0)) + 1;

            graph.SetEdge(top, childTop, new EdgeLabel
            {
                Weight = thisWeight,
                Minlen = (int) minlen,
                NestingEdge = true
            });

            graph.SetEdge(childBottom, bottom, new EdgeLabel
            {
                Weight = thisWeight,
                Minlen = (int) minlen,
                NestingEdge = true
            });
        }

        if (graph.Parent(v) == null)
        {
            graph.SetEdge(root, top, new EdgeLabel { Weight = 0, Minlen = (int) (height + (depths.GetValueOrDefault(v, 0))) });
        }
    }

    static Dictionary<string, int> TreeDepths(Graph graph)
    {
        var depths = new Dictionary<string, int>(StringComparer.Ordinal);

        void Dfs(string v, int depth)
        {
            var children = graph.Children(v);
            if (children.Count != 0)
            {
                foreach (var child in children)
                {
                    Dfs(child, depth + 1);
                }
            }

            depths[v] = depth;
        }

        foreach (var v in graph.Children(Util.GraphNode))
        {
            Dfs(v, 1);
        }

        return depths;
    }

    static double SumWeights(Graph graph)
    {
        // The real pipeline guarantees every edge has a weight; the unit tests can leave it
        // unset, mirroring JS where `acc + undefined` is NaN (and the resulting weight is unused
        // for those edges' assertions).
        var acc = 0d;
        foreach (var e in graph.Edges())
        {
            acc += graph.Edge_(e).Weight ?? double.NaN;
        }

        return acc;
    }

    public static void Cleanup(Graph graph)
    {
        var graphLabel = graph.Graph_();
        graph.RemoveNode(graphLabel.NestingRoot!);
        graphLabel.NestingRoot = null;
        foreach (var e in graph.Edges())
        {
            var edge = graph.Edge_(e);
            if (edge.NestingEdge == true)
            {
                graph.RemoveEdge(e);
            }
        }
    }
}
