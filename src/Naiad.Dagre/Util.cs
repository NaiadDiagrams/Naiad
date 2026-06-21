namespace Naiad.Dagre;

/// <summary>Faithful port of dagre's <c>util.ts</c> helpers.</summary>
static class Util
{
    public const string GraphNode = "\x00";

    static int idCounter;

    public static string UniqueId(string prefix) => prefix + (++idCounter).ToString(System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>Adds a dummy node to the graph and returns its id.</summary>
    public static string AddDummyNode(Graph graph, string type, NodeLabel attrs, string name)
    {
        var v = name;
        while (graph.HasNode(v))
        {
            v = UniqueId(name);
        }

        attrs.Dummy = type;
        graph.SetNode(v, attrs);
        return v;
    }

    public static string AddBorderNode(Graph graph, string prefix) =>
        AddDummyNode(graph, "border", new NodeLabel { Width = 0, Height = 0 }, prefix);

    public static string AddBorderNode(Graph graph, string prefix, int rank, int order) =>
        AddDummyNode(graph, "border", new NodeLabel { Width = 0, Height = 0, Rank = rank, Order = order }, prefix);

    /// <summary>Returns a new graph with only simple edges; aggregates multi-edge weight/minlen.</summary>
    public static Graph Simplify(Graph graph)
    {
        var simplified = new Graph().SetGraph(graph.Graph_());
        foreach (var v in graph.Nodes())
        {
            simplified.SetNode(v, graph.Node(v));
        }

        foreach (var e in graph.Edges())
        {
            var simpleLabel = simplified.HasEdge(e.V, e.W) ? simplified.Edge_(e.V, e.W) : new EdgeLabel { Weight = 0, Minlen = 1 };
            var label = graph.Edge_(e);
            simplified.SetEdge(e.V, e.W, new EdgeLabel
            {
                // JS treats a missing weight/minlen as undefined; matching dagre's behavior in the
                // network-simplex tests (which override an edge label with only {minlen}), we fall back
                // to the same defaults used for a fresh simple edge (weight 0, minlen 1).
                Weight = (simpleLabel.Weight ?? 0) + (label.Weight ?? 0),
                Minlen = Math.Max(simpleLabel.Minlen ?? 1, label.Minlen ?? 1)
            });
        }

        return simplified;
    }

    public static Graph AsNonCompoundGraph(Graph graph)
    {
        var simplified = new Graph(multigraph: graph.IsMultigraph()).SetGraph(graph.Graph_());
        foreach (var v in graph.Nodes())
        {
            if (graph.Children(v).Count == 0)
            {
                simplified.SetNode(v, graph.Node(v));
            }
        }

        foreach (var e in graph.Edges())
        {
            simplified.SetEdge(e, graph.Edge_(e));
        }

        return simplified;
    }

    public static Dictionary<string, Dictionary<string, double>> SuccessorWeights(Graph graph)
    {
        var result = new Dictionary<string, Dictionary<string, double>>(StringComparer.Ordinal);
        foreach (var v in graph.Nodes())
        {
            var sucs = new Dictionary<string, double>(StringComparer.Ordinal);
            var outEdges = graph.OutEdges(v);
            if (outEdges != null)
            {
                foreach (var e in outEdges)
                {
                    sucs[e.W] = sucs.GetValueOrDefault(e.W) + graph.Edge_(e).Weight!.Value;
                }
            }

            result[v] = sucs;
        }

        return result;
    }

    public static Dictionary<string, Dictionary<string, double>> PredecessorWeights(Graph graph)
    {
        var result = new Dictionary<string, Dictionary<string, double>>(StringComparer.Ordinal);
        foreach (var v in graph.Nodes())
        {
            var preds = new Dictionary<string, double>(StringComparer.Ordinal);
            var inEdges = graph.InEdges(v);
            if (inEdges != null)
            {
                foreach (var e in inEdges)
                {
                    preds[e.V] = preds.GetValueOrDefault(e.V) + graph.Edge_(e).Weight!.Value;
                }
            }

            result[v] = preds;
        }

        return result;
    }

    /// <summary>Where a line from <paramref name="point"/> toward the rect's center crosses the rect border.</summary>
    public static Point IntersectRect(NodeLabel rect, Point point)
    {
        var x = rect.X!.Value;
        var y = rect.Y!.Value;

        var dx = point.X - x;
        var dy = point.Y - y;
        var w = rect.Width / 2;
        var h = rect.Height / 2;

        if (dx == 0 && dy == 0)
        {
            throw new InvalidOperationException("Not possible to find intersection inside of the rectangle");
        }

        double sx;
        double sy;
        if (Math.Abs(dy) * w > Math.Abs(dx) * h)
        {
            if (dy < 0)
            {
                h = -h;
            }

            sx = h * dx / dy;
            sy = h;
        }
        else
        {
            if (dx < 0)
            {
                w = -w;
            }

            sx = w;
            sy = w * dy / dx;
        }

        return new Point(x + sx, y + sy);
    }

    /// <summary>Builds a matrix of node ids indexed by [rank][order].</summary>
    public static List<List<string>> BuildLayerMatrix(Graph graph)
    {
        var rankCount = (int) (MaxRank(graph) + 1);
        var layering = new List<List<string>>();
        for (var i = 0; i < rankCount; i++)
        {
            layering.Add([]);
        }

        foreach (var v in graph.Nodes())
        {
            var node = graph.Node(v);
            if (node.Rank is { } rank)
            {
                while (layering.Count <= rank)
                {
                    layering.Add([]);
                }

                var row = layering[rank];
                var order = node.Order!.Value;
                while (row.Count <= order)
                {
                    row.Add(null!);
                }

                row[order] = v;
            }
        }

        return layering;
    }

    public static void NormalizeRanks(Graph graph)
    {
        var nodeRanks = graph.Nodes().Select(v => graph.Node(v).Rank is { } r ? (double) r : double.MaxValue).ToList();
        var min = ApplyMin(nodeRanks);
        foreach (var v in graph.Nodes())
        {
            var node = graph.Node(v);
            if (node.Rank.HasValue)
            {
                node.Rank -= (int) min;
            }
        }
    }

    public static void RemoveEmptyRanks(Graph graph)
    {
        var nodeRanks = graph.Nodes().Select(v => graph.Node(v).Rank).Where(r => r.HasValue).Select(r => (double) r!.Value).ToList();
        var offset = (int) ApplyMin(nodeRanks);

        var layers = new List<List<string>?>();
        foreach (var v in graph.Nodes())
        {
            // In the TS, a node with an undefined rank (e.g. a compound subgraph parent) yields
            // `undefined - offset === NaN`; `layers[NaN]` lands in a non-numeric property bucket that
            // the later numeric-index iteration never visits. Mirror that by skipping such nodes.
            if (graph.Node(v).Rank is not { } rankValue)
            {
                continue;
            }

            var rank = rankValue - offset;
            while (layers.Count <= rank)
            {
                layers.Add(null);
            }

            (layers[rank] ??= []).Add(v);
        }

        var delta = 0;
        var nodeRankFactor = graph.Graph_().NodeRankFactor!.Value;
        for (var i = 0; i < layers.Count; i++)
        {
            var vs = layers[i];
            if (vs == null && i % nodeRankFactor != 0)
            {
                --delta;
            }
            else if (vs != null && delta != 0)
            {
                foreach (var v in vs)
                {
                    graph.Node(v).Rank += delta;
                }
            }
        }
    }

    public static double MaxRank(Graph graph)
    {
        var nodeRanks = graph.Nodes().Select(v => graph.Node(v).Rank is { } r ? (double) r : double.Epsilon).ToList();
        return ApplyMax(nodeRanks);
    }

    public static PartitionResult<T> Partition<T>(IEnumerable<T> collection, Func<T, bool> fn)
    {
        var result = new PartitionResult<T>();
        foreach (var value in collection)
        {
            if (fn(value))
            {
                result.Lhs.Add(value);
            }
            else
            {
                result.Rhs.Add(value);
            }
        }

        return result;
    }

    public static T Time<T>(string name, Func<T> fn) => fn();

    public static T Notime<T>(string name, Func<T> fn) => fn();

    public static List<int> Range(int limit) => Range(0, limit, 1);

    public static List<int> Range(int start, int limit, int step = 1)
    {
        var range = new List<int>();
        if (step < 0)
        {
            for (var i = start; limit < i; i += step)
            {
                range.Add(i);
            }
        }
        else
        {
            for (var i = start; i < limit; i += step)
            {
                range.Add(i);
            }
        }

        return range;
    }

    public static Dictionary<string, R> MapValues<T, R>(IEnumerable<KeyValuePair<string, T>> obj, Func<T, string, R> fn)
    {
        var acc = new Dictionary<string, R>(StringComparer.Ordinal);
        foreach (var (k, v) in obj)
        {
            acc[k] = fn(v, k);
        }

        return acc;
    }

    public static Dictionary<string, T> ZipObject<T>(IReadOnlyList<string> props, IReadOnlyList<T> values)
    {
        var acc = new Dictionary<string, T>(StringComparer.Ordinal);
        for (var i = 0; i < props.Count; i++)
        {
            acc[props[i]] = values[i];
        }

        return acc;
    }

    // applyWithChunking(Math.max/Math.min, ...): empty reduces to ±Infinity as in JS.
    public static double ApplyMax(IReadOnlyList<double> values) => values.Count == 0 ? double.NegativeInfinity : values.Max();

    public static double ApplyMin(IReadOnlyList<double> values) => values.Count == 0 ? double.PositiveInfinity : values.Min();
}
