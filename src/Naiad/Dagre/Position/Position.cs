namespace Naiad.Dagre;

static class Position
{
    internal static void Run(Graph graph)
    {
        graph = Util.AsNonCompoundGraph(graph);

        PositionY(graph);
        foreach (var (v, x) in BK.PositionX(graph))
        {
            graph.Node(v).X = x;
        }
    }

    internal static void PositionY(Graph graph)
    {
        var layering = Util.BuildLayerMatrix(graph);
        var graphLabel = graph.Graph_();
        var rankSep = graphLabel.Ranksep!.Value;
        var rankAlign = graphLabel.Rankalign;
        double prevY = 0;
        foreach (var layer in layering)
        {
            double maxHeight = 0;
            foreach (var v in layer)
            {
                maxHeight = Math.Max(maxHeight, graph.Node(v).Height);
            }

            foreach (var v in layer)
            {
                var node = graph.Node(v);
                if (rankAlign == "top")
                {
                    node.Y = prevY + node.Height / 2;
                }
                else if (rankAlign == "bottom")
                {
                    node.Y = prevY + maxHeight - node.Height / 2;
                }
                else
                {
                    node.Y = prevY + maxHeight / 2;
                }
            }

            prevY += maxHeight + rankSep;
        }
    }
}
