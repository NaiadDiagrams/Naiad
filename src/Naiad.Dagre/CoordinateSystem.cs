namespace Naiad.Dagre;

static class CoordinateSystem
{
    public static void Adjust(Graph graph)
    {
        var rankDir = graph.Graph_().Rankdir?.ToLowerInvariant();
        if (rankDir == "lr" || rankDir == "rl")
        {
            SwapWidthHeight(graph);
        }
    }

    public static void Undo(Graph graph)
    {
        var rankDir = graph.Graph_().Rankdir?.ToLowerInvariant();
        if (rankDir == "bt" || rankDir == "rl")
        {
            ReverseY(graph);
        }

        if (rankDir == "lr" || rankDir == "rl")
        {
            SwapXY(graph);
            SwapWidthHeight(graph);
        }
    }

    internal static void SwapWidthHeight(Graph graph)
    {
        graph.Nodes().ForEach(node => SwapWidthHeightOne(graph.Node(node)));
        graph.Edges().ForEach(edge => SwapWidthHeightOne(graph.Edge_(edge)));
    }

    internal static void SwapWidthHeightOne(NodeLabel attrs)
    {
        var w = attrs.Width;
        attrs.Width = attrs.Height;
        attrs.Height = w;
    }

    internal static void SwapWidthHeightOne(EdgeLabel attrs)
    {
        var w = attrs.Width;
        attrs.Width = attrs.Height;
        attrs.Height = w;
    }

    internal static void ReverseY(Graph graph)
    {
        graph.Nodes().ForEach(node => ReverseYOne(graph.Node(node)));

        graph.Edges().ForEach(edge =>
        {
            var edgeLabel = graph.Edge_(edge);
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

    internal static void ReverseYOne(NodeLabel attrs) => attrs.Y = -attrs.Y!.Value;

    internal static void ReverseYOne(EdgeLabel attrs) => attrs.Y = -attrs.Y!.Value;

    internal static Point ReverseYOne(Point attrs)
    {
        attrs.Y = -attrs.Y;
        return attrs;
    }

    internal static void SwapXY(Graph graph)
    {
        graph.Nodes().ForEach(node => SwapXYOne(graph.Node(node)));

        graph.Edges().ForEach(edge =>
        {
            var edgeLabel = graph.Edge_(edge);
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

    internal static void SwapXYOne(NodeLabel attrs)
    {
        var x = attrs.X;
        attrs.X = attrs.Y;
        attrs.Y = x;
    }

    internal static void SwapXYOne(EdgeLabel attrs)
    {
        var x = attrs.X;
        attrs.X = attrs.Y;
        attrs.Y = x;
    }

    internal static Point SwapXYOne(Point attrs)
    {
        var x = attrs.X;
        attrs.X = attrs.Y;
        attrs.Y = x;
        return attrs;
    }
}
