namespace Naiad.Dagre;

/// <summary>Faithful port of dagre's <c>order/cross-count.ts</c>.</summary>
static class CrossCount
{
    public static int Run(Graph graph, List<List<string>> layering)
    {
        var cc = 0;
        for (var i = 1; i < layering.Count; ++i)
        {
            cc += TwoLayerCrossCount(graph, layering[i - 1], layering[i]);
        }

        return cc;
    }

    sealed class SouthEntry
    {
        public int Pos;
        public double Weight;
    }

    static int TwoLayerCrossCount(Graph graph, List<string> northLayer, List<string> southLayer)
    {
        // Sort all of the edges between the north and south layers by their position
        // in the north layer and then the south. Map these edges to the position of
        // their head in the south layer.
        var southPos = Util.ZipObject(southLayer, Util.Range(southLayer.Count));
        var southEntries = new List<SouthEntry>();
        foreach (var v in northLayer)
        {
            var edges = graph.OutEdges(v);
            if (edges == null)
            {
                continue;
            }

            var mapped = edges.Select(e => new SouthEntry
            {
                Pos = southPos[e.W],
                Weight = graph.Edge_(e).Weight!.Value
            }).ToList();
            mapped.Sort((a, b) => a.Pos - b.Pos);
            southEntries.AddRange(mapped);
        }

        // Build the accumulator tree
        var firstIndex = 1;
        while (firstIndex < southLayer.Count)
        {
            firstIndex <<= 1;
        }

        var treeSize = 2 * firstIndex - 1;
        firstIndex -= 1;
        var tree = new double[treeSize];

        // Calculate the weighted crossings
        var cc = 0d;
        foreach (var entry in southEntries)
        {
            var index = entry.Pos + firstIndex;
            tree[index] += entry.Weight;
            var weightSum = 0d;
            while (index > 0)
            {
                if (index % 2 != 0)
                {
                    weightSum += tree[index + 1];
                }

                index = (index - 1) >> 1;
                tree[index] += entry.Weight;
            }

            cc += entry.Weight * weightSum;
        }

        return (int) cc;
    }
}
