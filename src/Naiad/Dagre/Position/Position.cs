namespace Naiad.Dagre;

static class Positioning
{
    internal static void Run(Graph graph)
    {
        graph = Util.AsNonCompoundGraph(graph);

        PositionY(graph);
        foreach (var (v, x) in BK.PositionX(graph))
        {
            graph.NodeLabel(v).X = x;
        }
    }

    static void PositionY(Graph graph)
    {
        var layering = Util.BuildLayerMatrix(graph);
        var graphLabel = graph.Label;
        var rankSep = graphLabel.RankSeparation!.Value;
        double prevY = 0;
        foreach (var layer in layering)
        {
            double maxHeight = 0;
            foreach (var v in layer)
            {
                maxHeight = Math.Max(maxHeight, graph.NodeLabel(v).Height);
            }

            foreach (var v in layer)
            {
                var node = graph.NodeLabel(v);
                node.Y = prevY + maxHeight / 2;
            }

            prevY += maxHeight + rankSep;
        }
    }
}
