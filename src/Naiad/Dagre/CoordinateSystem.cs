using Naiad.Dagre;

static class CoordinateSystem
{
    public static void Adjust(Graph graph)
    {
        var rankDir = graph.Label.Rankdir;
        if (rankDir is Direction.LeftToRight or Direction.RightToLeft)
        {
            SwapWidthHeight(graph);
        }
    }

    public static void Undo(Graph graph)
    {
        var rankDir = graph.Label.Rankdir;
        if (rankDir is Direction.BottomToTop or Direction.RightToLeft)
        {
            ReverseY(graph);
        }

        if (rankDir is Direction.LeftToRight or Direction.RightToLeft)
        {
            SwapXY(graph);
            SwapWidthHeight(graph);
        }
    }

    static void SwapWidthHeight(Graph graph)
    {
        graph.Nodes().ForEach(node => SwapWidthHeightOne(graph.NodeLabel(node)));
        graph.EdgeLabels().ForEach(SwapWidthHeightOne);
    }

    static void SwapWidthHeightOne(NodeLabel attrs) =>
        (attrs.Width, attrs.Height) = (attrs.Height, attrs.Width);

    static void SwapWidthHeightOne(EdgeLabel attrs) =>
        (attrs.Width, attrs.Height) = (attrs.Height, attrs.Width);

    static void ReverseY(Graph graph)
    {
        graph.Nodes().ForEach(node => ReverseYOne(graph.NodeLabel(node)));

        graph.EdgeLabels().ForEach(edgeLabel =>
        {
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

    static Position ReverseYOne(Position attrs) => attrs with { Y = -attrs.Y };

    static void SwapXY(Graph graph)
    {
        graph.Nodes().ForEach(node => SwapXYOne(graph.NodeLabel(node)));

        graph.EdgeLabels().ForEach(edgeLabel =>
        {
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

    static void SwapXYOne(NodeLabel attrs) =>
        (attrs.X, attrs.Y) = (attrs.Y, attrs.X);

    static void SwapXYOne(EdgeLabel attrs) =>
        (attrs.X, attrs.Y) = (attrs.Y, attrs.X);

    static Position SwapXYOne(Position attrs) => attrs with { X = attrs.Y, Y = attrs.X };
}
