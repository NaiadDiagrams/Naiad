namespace Naiad.Dagre;

/*
 * Breaks any long edges in the graph into short segments that span 1 layer
 * each. This operation is undoable with the denormalize function.
 *
 * Pre-conditions:
 *
 *    1. The input graph is a DAG.
 *    2. Each node in the graph has a "rank" property.
 *
 * Post-condition:
 *
 *    1. All edges in the graph have a length of 1.
 *    2. Dummy nodes are added where edges have been split into segments.
 *    3. The graph is augmented with a "dummyChains" attribute which contains
 *       the first dummy in each chain of dummy nodes produced.
 */
static class Normalize
{
    public static void Run(Graph graph)
    {
        graph.Label.DummyChains = [];
        foreach (var edge in graph.Edges())
        {
            NormalizeEdge(graph, edge);
        }
    }

    static void NormalizeEdge(Graph graph, Edge e)
    {
        var v = e.V;
        var vRank = graph.NodeLabel(v).Rank!.Value;
        var w = e.W;
        var wRank = graph.NodeLabel(w).Rank!.Value;
        var name = e.Name;
        var edgeLabel = graph.FindEdgeLabel(e);
        var labelRank = edgeLabel.LabelRank;

        if (wRank == vRank + 1)
        {
            return;
        }

        graph.RemoveEdge(e);

        // The first ++vRank lives in the initializer (runs once); the increment in the
        // update clause runs after each body. Expressed as a while loop to avoid a
        // duplicate increment in the condition.
        var i = 0;
        ++vRank;
        for (; vRank < wRank; ++i, ++vRank)
        {
            edgeLabel.Points = [];
            var attrs = new NodeLabel
            {
                Width = 0,
                Height = 0,
                EdgeLabel = edgeLabel,
                EdgeObj = e,
                Rank = vRank
            };
            var dummy = Util.AddDummyNode(graph, DummyKind.Edge, attrs, "_d");
            if (vRank == labelRank)
            {
                attrs.Width = edgeLabel.Width ?? 0;
                attrs.Height = edgeLabel.Height ?? 0;
                attrs.Dummy = DummyKind.EdgeLabel;
                attrs.Labelpos = edgeLabel.Labelpos;
            }

            graph.SetEdge(v, dummy, new() { Weight = edgeLabel.Weight }, name);
            if (i == 0)
            {
                graph.Label.DummyChains!.Add(dummy);
            }

            v = dummy;
        }

        graph.SetEdge(v, w, new() { Weight = edgeLabel.Weight }, name);
    }

    public static void Undo(Graph graph)
    {
        foreach (var start in graph.Label.DummyChains!)
        {
            var v = start;
            var node = graph.NodeLabel(v);
            var origLabel = node.EdgeLabel!;
            graph.SetEdge(node.EdgeObj!, origLabel);
            while (node.Dummy != null)
            {
                var w = graph.Successors(v)![0];
                graph.RemoveNode(v);
                // Position is a non-nullable struct, so coerce a not-yet-positioned dummy's coordinates to 0.
                // Callers assign coordinates before calling Undo, so this fallback is never observed.
                origLabel.Points!.Add(new(node.X ?? 0, node.Y ?? 0));
                if (node.Dummy == DummyKind.EdgeLabel)
                {
                    origLabel.X = node.X;
                    origLabel.Y = node.Y;
                    origLabel.Width = node.Width;
                    origLabel.Height = node.Height;
                }

                v = w;
                node = graph.NodeLabel(v);
            }
        }
    }
}
