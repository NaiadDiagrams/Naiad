namespace Naiad.Dagre;

/// <summary>
/// An entry threaded into the greedy-FAS bucket lists. A faithful port of the TS <c>FASEntry</c>
/// object (<c>{v, in, out}</c>); <see cref="W"/> is used when entries are emitted as edges from
/// <see cref="GreedyFas.RemoveNode"/>.
/// </summary>
sealed class FasEntry : ListNode
{
    public string V = "";
    public string? W;
    public double In;
    public double Out;
}

/// <summary>
/// A greedy heuristic for finding a feedback arc set for a graph. A feedback arc set is a set of
/// edges that can be removed to make a graph acyclic. The algorithm comes from: P. Eades, X. Lin,
/// and W. F. Smyth, "A fast and effective heuristic for the feedback arc set problem." This
/// implementation adjusts that from the paper to allow for weighted edges. Faithful port of
/// <c>greedy-fas.ts</c>.
/// </summary>
static class GreedyFas
{
    static readonly Func<Edge, double> DefaultWeightFn = _ => 1;

    sealed class FasState
    {
        public Graph Graph = null!;
        public Dictionary<string, FasEntry> Entries = null!;
        public List<DoublyLinkedList> Buckets = null!;
        public int ZeroIdx;
    }

    public static List<Edge> Run(Graph graph, Func<Edge, double>? weightFn = null)
    {
        if (graph.NodeCount() <= 1)
        {
            return [];
        }

        var state = BuildState(graph, weightFn ?? DefaultWeightFn);
        var results = DoGreedyFas(state.Graph, state.Entries, state.Buckets, state.ZeroIdx);

        // Expand multi-edges
        return results.SelectMany(edge => graph.OutEdges(edge.V, edge.W) ?? []).ToList();
    }

    static List<Edge> DoGreedyFas(Graph g, Dictionary<string, FasEntry> entries, List<DoublyLinkedList> buckets, int zeroIdx)
    {
        var results = new List<Edge>();
        var sources = buckets[^1];
        var sinks = buckets[0];

        FasEntry? entry;
        while (g.NodeCount() != 0)
        {
            while ((entry = (FasEntry?)sinks.Dequeue()) != null)
            {
                RemoveNode(g, entries, buckets, zeroIdx, entry);
            }

            while ((entry = (FasEntry?)sources.Dequeue()) != null)
            {
                RemoveNode(g, entries, buckets, zeroIdx, entry);
            }

            if (g.NodeCount() != 0)
            {
                for (var i = buckets.Count - 2; i > 0; --i)
                {
                    entry = (FasEntry?)buckets[i].Dequeue();
                    if (entry != null)
                    {
                        results = results.Concat(RemoveNode(g, entries, buckets, zeroIdx, entry, true) ?? []).ToList();
                        break;
                    }
                }
            }
        }

        return results;
    }

    static List<Edge>? RemoveNode(
        Graph graph,
        Dictionary<string, FasEntry> entries,
        List<DoublyLinkedList> buckets,
        int zeroIdx,
        FasEntry entry,
        bool collectPredecessors = false)
    {
        var collected = new List<Edge>();
        var results = collectPredecessors ? collected : null;

        foreach (var edge in graph.InEdges(entry.V) ?? [])
        {
            var weight = graph.Edge_(edge).Weight!.Value;
            var uEntry = entries[edge.V];

            if (collectPredecessors)
            {
                collected.Add(new(edge.V, edge.W));
            }

            uEntry.Out -= weight;
            AssignBucket(buckets, zeroIdx, uEntry);
        }

        foreach (var edge in graph.OutEdges(entry.V) ?? [])
        {
            var weight = graph.Edge_(edge).Weight!.Value;
            var w = edge.W;
            var wEntry = entries[w];
            wEntry.In -= weight;
            AssignBucket(buckets, zeroIdx, wEntry);
        }

        graph.RemoveNode(entry.V);

        return results;
    }

    static FasState BuildState(Graph graph, Func<Edge, double> weightFn)
    {
        var fasGraph = new Graph();
        var entries = new Dictionary<string, FasEntry>(StringComparer.Ordinal);
        var maxIn = 0d;
        var maxOut = 0d;

        foreach (var v in graph.Nodes())
        {
            var entry = new FasEntry { V = v, In = 0, Out = 0 };
            entries[v] = entry;
            fasGraph.SetNode(v, new());
        }

        // Aggregate weights on nodes, but also sum the weights across multi-edges
        // into a single edge for the fasGraph.
        foreach (var edge in graph.Edges())
        {
            var prevWeight = fasGraph.Edge_(edge.V, edge.W)?.Weight ?? 0;
            var weight = weightFn(edge);
            var edgeWeight = prevWeight + weight;
            fasGraph.SetEdge(edge.V, edge.W, new() { Weight = edgeWeight });
            var vNode = entries[edge.V];
            var wNode = entries[edge.W];
            maxOut = Math.Max(maxOut, vNode.Out += weight);
            maxIn = Math.Max(maxIn, wNode.In += weight);
        }

        var buckets = Util.Range((int)(maxOut + maxIn + 3)).Select(_ => new DoublyLinkedList()).ToList();
        var zeroIdx = (int)(maxIn + 1);

        foreach (var v in fasGraph.Nodes())
        {
            AssignBucket(buckets, zeroIdx, entries[v]);
        }

        return new() { Graph = fasGraph, Entries = entries, Buckets = buckets, ZeroIdx = zeroIdx };
    }

    static void AssignBucket(List<DoublyLinkedList> buckets, int zeroIdx, FasEntry entry)
    {
        if (entry.Out == 0)
        {
            buckets[0].Enqueue(entry);
        }
        else if (entry.In == 0)
        {
            buckets[^1].Enqueue(entry);
        }
        else
        {
            buckets[(int)(entry.Out - entry.In) + zeroIdx].Enqueue(entry);
        }
    }
}
