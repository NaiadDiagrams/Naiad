using Naiad.Dagre;

static class CoordinateSystem
{
    public static void Adjust(Graph graph)
    {
        var rankDir = graph.GraphLabel.Rankdir?.ToLowerInvariant();
        if (rankDir is "lr" or "rl")
        {
            SwapWidthHeight(graph);
        }
    }

    public static void Undo(Graph graph)
    {
        var rankDir = graph.GraphLabel.Rankdir?.ToLowerInvariant();
        if (rankDir is "bt" or "rl")
        {
            ReverseY(graph);
        }

        if (rankDir is "lr" or "rl")
        {
            SwapXY(graph);
            SwapWidthHeight(graph);
        }
    }

    static void SwapWidthHeight(Graph graph)
    {
        graph.Nodes().ForEach(node => SwapWidthHeightOne(graph.NodeLabel(node)));
        graph.Edges().ForEach(edge => SwapWidthHeightOne(graph.FindEdgeLabel(edge)));
    }

    static void SwapWidthHeightOne(NodeLabel attrs)
    {
        var w = attrs.Width;
        attrs.Width = attrs.Height;
        attrs.Height = w;
    }

    static void SwapWidthHeightOne(EdgeLabel attrs)
    {
        var w = attrs.Width;
        attrs.Width = attrs.Height;
        attrs.Height = w;
    }

    static void ReverseY(Graph graph)
    {
        graph.Nodes().ForEach(node => ReverseYOne(graph.NodeLabel(node)));

        graph.Edges().ForEach(edge =>
        {
            var edgeLabel = graph.FindEdgeLabel(edge);
            if (edgeLabel.Points != null)
            {
                for (var i = 0; i < edgeLabel.Points.Count; i++)
                {
                    edgeLabel.Points[i] = ReverseYOne(edgeLabel.Points[i]);
                }
            }

            if (edgeLabel.Y != null)
            {
                ReverseYOne(edgeLabel);
            }
        });
    }

    static void ReverseYOne(NodeLabel attrs) => attrs.Y = -attrs.Y!.Value;

    static void ReverseYOne(EdgeLabel attrs) => attrs.Y = -attrs.Y!.Value;

    static Point ReverseYOne(Point attrs)
    {
        attrs.Y = -attrs.Y;
        return attrs;
    }

    static void SwapXY(Graph graph)
    {
        graph.Nodes().ForEach(node => SwapXYOne(graph.NodeLabel(node)));

        graph.Edges().ForEach(edge =>
        {
            var edgeLabel = graph.FindEdgeLabel(edge);
            if (edgeLabel.Points != null)
            {
                for (var i = 0; i < edgeLabel.Points.Count; i++)
                {
                    edgeLabel.Points[i] = SwapXYOne(edgeLabel.Points[i]);
                }
            }

            if (edgeLabel.X != null)
            {
                SwapXYOne(edgeLabel);
            }
        });
    }

    static void SwapXYOne(NodeLabel attrs)
    {
        var x = attrs.X;
        attrs.X = attrs.Y;
        attrs.Y = x;
    }

    static void SwapXYOne(EdgeLabel attrs)
    {
        var x = attrs.X;
        attrs.X = attrs.Y;
        attrs.Y = x;
    }

    static Point SwapXYOne(Point attrs)
    {
        var x = attrs.X;
        attrs.X = attrs.Y;
        attrs.Y = x;
        return attrs;
    }
}
