namespace Naiad.Dagre;

/// <summary>Top-level layout driver.</summary>
static class Layout
{
    public static Graph Run(Graph g)
    {
        var layoutGraph = BuildLayoutGraph(g);
        RunLayout(layoutGraph);
        UpdateInputGraph(g, layoutGraph);
        return layoutGraph;
    }

    static void RunLayout(Graph g)
    {
        MakeSpaceForEdgeLabels(g);
        RemoveSelfEdges(g);
        Acyclic.Run(g);
        NestingGraph.Run(g);
        Rank.Run(Util.AsNonCompoundGraph(g));
        InjectEdgeLabelProxies(g);
        Util.RemoveEmptyRanks(g);
        NestingGraph.Cleanup(g);
        Util.NormalizeRanks(g);
        AssignRankMinMax(g);
        RemoveEdgeLabelProxies(g);
        Normalize.Run(g);
        ParentDummyChains.Run(g);
        AddBorderSegments.Run(g);
        Order.Run(g);
        InsertSelfEdges(g);
        CoordinateSystem.Adjust(g);
        Position.Run(g);
        PositionSelfEdges(g);
        RemoveBorderNodes(g);
        Normalize.Undo(g);
        FixupEdgeLabelCoords(g);
        CoordinateSystem.Undo(g);
        TranslateGraph(g);
        AssignNodeIntersects(g);
        ReversePointsForReversedEdges(g);
        Acyclic.Undo(g);
    }

    /*
     * Copies final layout information from the layout graph back to the input
     * graph. This process only copies whitelisted attributes from the layout graph
     * to the input graph, so it serves as a good place to determine what
     * attributes can influence layout.
     */
    static void UpdateInputGraph(Graph inputGraph, Graph layoutGraph)
    {
        foreach (var v in inputGraph.Nodes())
        {
            var inputLabel = inputGraph.Node(v);
            var layoutLabel = layoutGraph.Node(v);

            if (inputLabel != null)
            {
                inputLabel.X = layoutLabel.X;
                inputLabel.Y = layoutLabel.Y;
                inputLabel.Order = layoutLabel.Order;
                inputLabel.Rank = layoutLabel.Rank;

                if (layoutGraph.Children(v).Count != 0)
                {
                    inputLabel.Width = layoutLabel.Width;
                    inputLabel.Height = layoutLabel.Height;
                }
            }
        }

        foreach (var e in inputGraph.Edges())
        {
            var inputLabel = inputGraph.FindEdgeLabel(e);
            var layoutLabel = layoutGraph.FindEdgeLabel(e);

            inputLabel.Points = layoutLabel.Points;
            if (layoutLabel.X.HasValue)
            {
                inputLabel.X = layoutLabel.X;
                inputLabel.Y = layoutLabel.Y;
            }
        }

        inputGraph.GraphLabel.Width = layoutGraph.GraphLabel.Width;
        inputGraph.GraphLabel.Height = layoutGraph.GraphLabel.Height;
    }

    /// <summary>
    /// Builds the graph used for layout: copies only the attributes that can influence layout from the
    /// input graph onto a fresh graph, filling in defaults for anything the caller left unset.
    /// </summary>
    static Graph BuildLayoutGraph(Graph inputGraph)
    {
        var g = new Graph(multigraph: true, compound: true);
        var graph = inputGraph.GraphLabel;

        var newGraph = new GraphLabel
        {
            // defaults
            Ranksep = 50,
            Edgesep = 20,
            Nodesep = 50,
            Rankdir = "TB",
            Rankalign = "center"
        };
        if (graph?.Nodesep is { } gNodesep) newGraph.Nodesep = gNodesep;
        if (graph?.Edgesep is { } gEdgesep) newGraph.Edgesep = gEdgesep;
        if (graph?.Ranksep is { } gRanksep) newGraph.Ranksep = gRanksep;
        if (graph?.Marginx is { } gMarginx) newGraph.Marginx = gMarginx;
        if (graph?.Marginy is { } gMarginy) newGraph.Marginy = gMarginy;
        if (graph?.Acyclicer is { } gAcyclicer) newGraph.Acyclicer = gAcyclicer;
        if (graph?.Ranker is { } gRanker) newGraph.Ranker = gRanker;
        if (graph?.Rankdir is { } gRankdir) newGraph.Rankdir = gRankdir;
        if (graph?.Align is { } gAlign) newGraph.Align = gAlign;
        if (graph?.Rankalign is { } gRankalign) newGraph.Rankalign = gRankalign;
        g.SetGraph(newGraph);

        foreach (var v in inputGraph.Nodes())
        {
            var node = inputGraph.Node(v);
            // Implicit nodes created via SetParent have no label; default a null label to width/height 0.
            var newNode = node == null
                ? new NodeLabel { Width = 0, Height = 0 }
                : new NodeLabel
                {
                    Width = node.Width,
                    Height = node.Height,
                    Rank = node.Rank
                };

            g.SetNode(v, newNode);
            var parent = inputGraph.Parent(v);
            if (parent != null)
            {
                g.SetParent(v, parent);
            }
        }

        foreach (var e in inputGraph.Edges())
        {
            var edge = inputGraph.FindEdgeLabel(e);
            var newEdge = new EdgeLabel
            {
                // defaults
                Minlen = 1,
                Weight = 1,
                Width = 0,
                Height = 0,
                Labeloffset = 10,
                Labelpos = "r"
            };
            if (edge.Minlen is { } eMinlen) newEdge.Minlen = eMinlen;
            if (edge.Weight is { } eWeight) newEdge.Weight = eWeight;
            if (edge.Width is { } eWidth) newEdge.Width = eWidth;
            if (edge.Height is { } eHeight) newEdge.Height = eHeight;
            if (edge.Labeloffset is { } eLabeloffset) newEdge.Labeloffset = eLabeloffset;
            if (edge.Labelpos is { } eLabelpos) newEdge.Labelpos = eLabelpos;

            g.SetEdge(e, newEdge);
        }

        return g;
    }

    /*
     * This idea comes from the Gansner paper: to account for edge labels in our
     * layout we split each rank in half by doubling minlen and halving ranksep.
     * Then we can place labels at these mid-points between nodes.
     *
     * We also add some minimal padding to the width to push the label for the edge
     * away from the edge itself a bit.
     */
    static void MakeSpaceForEdgeLabels(Graph g)
    {
        var graph = g.GraphLabel;
        graph.Ranksep /= 2;
        foreach (var e in g.Edges())
        {
            var edge = g.FindEdgeLabel(e);
            edge.Minlen *= 2;
            if (!edge.Labelpos!.Equals("c", StringComparison.OrdinalIgnoreCase))
            {
                if (graph.Rankdir == "TB" || graph.Rankdir == "BT")
                {
                    edge.Width += edge.Labeloffset;
                }
                else
                {
                    edge.Height += edge.Labeloffset;
                }
            }
        }
    }

    /*
     * Creates temporary dummy nodes that capture the rank in which each edge's
     * label is going to, if it has one of non-zero width and height. We do this
     * so that we can safely remove empty ranks while preserving balance for the
     * label's position.
     */
    static void InjectEdgeLabelProxies(Graph g)
    {
        foreach (var e in g.Edges())
        {
            var edge = g.FindEdgeLabel(e);
            if (edge.Width is { } width && width != 0 && edge.Height is { } height && height != 0)
            {
                var v = g.Node(e.V);
                var w = g.Node(e.W);
                // {rank: (w.rank - v.rank) / 2 + v.rank, e: e}
                var label = new NodeLabel
                {
                    Rank = (w.Rank!.Value - v.Rank!.Value) / 2 + v.Rank!.Value,
                    EdgeObj = e
                };
                Util.AddDummyNode(g, "edge-proxy", label, "_ep");
            }
        }
    }

    static void AssignRankMinMax(Graph g)
    {
        var maxRank = 0;
        foreach (var v in g.Nodes())
        {
            var node = g.Node(v);
            if (node.BorderTop != null)
            {
                node.MinRank = g.Node(node.BorderTop).Rank;
                node.MaxRank = g.Node(node.BorderBottom!).Rank;
                maxRank = Math.Max(maxRank, node.MaxRank!.Value);
            }
        }

        g.GraphLabel.MaxRank = maxRank;
    }

    static void RemoveEdgeLabelProxies(Graph g)
    {
        foreach (var v in g.Nodes())
        {
            var node = g.Node(v);
            if (node.Dummy == "edge-proxy")
            {
                g.FindEdgeLabel(node.EdgeObj!).LabelRank = node.Rank;
                g.RemoveNode(v);
            }
        }
    }

    static void TranslateGraph(Graph g)
    {
        var minX = double.PositiveInfinity;
        var maxX = 0.0;
        var minY = double.PositiveInfinity;
        var maxY = 0.0;
        var graphLabel = g.GraphLabel;
        var marginX = graphLabel.Marginx ?? 0;
        var marginY = graphLabel.Marginy ?? 0;

        void GetExtremes(double x, double y, double w, double h)
        {
            minX = Math.Min(minX, x - w / 2);
            maxX = Math.Max(maxX, x + w / 2);
            minY = Math.Min(minY, y - h / 2);
            maxY = Math.Max(maxY, y + h / 2);
        }

        foreach (var v in g.Nodes())
        {
            var node = g.Node(v);
            GetExtremes(node.X!.Value, node.Y!.Value, node.Width, node.Height);
        }

        foreach (var e in g.Edges())
        {
            var edge = g.FindEdgeLabel(e);
            if (edge.X.HasValue)
            {
                GetExtremes(edge.X!.Value, edge.Y!.Value, edge.Width!.Value, edge.Height!.Value);
            }
        }

        minX -= marginX;
        minY -= marginY;

        foreach (var v in g.Nodes())
        {
            var node = g.Node(v);
            node.X -= minX;
            node.Y -= minY;
        }

        foreach (var e in g.Edges())
        {
            var edge = g.FindEdgeLabel(e);
            var points = edge.Points!;
            for (var i = 0; i < points.Count; i++)
            {
                var p = points[i];
                p.X -= minX;
                p.Y -= minY;
                points[i] = p;
            }

            if (edge.X.HasValue)
            {
                edge.X -= minX;
            }

            if (edge.Y.HasValue)
            {
                edge.Y -= minY;
            }
        }

        graphLabel.Width = maxX - minX + marginX;
        graphLabel.Height = maxY - minY + marginY;
    }

    static void AssignNodeIntersects(Graph g)
    {
        foreach (var e in g.Edges())
        {
            var edge = g.FindEdgeLabel(e);
            var nodeV = g.Node(e.V);
            var nodeW = g.Node(e.W);
            Point p1;
            Point p2;
            if (edge.Points == null)
            {
                edge.Points = [];
                p1 = new(nodeW.X!.Value, nodeW.Y!.Value);
                p2 = new(nodeV.X!.Value, nodeV.Y!.Value);
            }
            else
            {
                p1 = edge.Points[0];
                p2 = edge.Points[edge.Points.Count - 1];
            }

            edge.Points.Insert(0, Util.IntersectRect(nodeV, p1));
            edge.Points.Add(Util.IntersectRect(nodeW, p2));
        }
    }

    static void FixupEdgeLabelCoords(Graph g)
    {
        foreach (var e in g.Edges())
        {
            var edge = g.FindEdgeLabel(e);
            if (edge.X.HasValue)
            {
                if (edge.Labelpos == "l" || edge.Labelpos == "r")
                {
                    edge.Width -= edge.Labeloffset;
                }

                switch (edge.Labelpos)
                {
                    case "l":
                        edge.X -= edge.Width!.Value / 2 + edge.Labeloffset;
                        break;
                    case "r":
                        edge.X += edge.Width!.Value / 2 + edge.Labeloffset;
                        break;
                }
            }
        }
    }

    static void ReversePointsForReversedEdges(Graph g)
    {
        foreach (var e in g.Edges())
        {
            var edge = g.FindEdgeLabel(e);
            if (edge.Reversed == true)
            {
                edge.Points!.Reverse();
            }
        }
    }

    static void RemoveBorderNodes(Graph g)
    {
        foreach (var v in g.Nodes())
        {
            if (g.Children(v).Count != 0)
            {
                var node = g.Node(v);
                var t = g.Node(node.BorderTop!);
                var b = g.Node(node.BorderBottom!);
                var l = g.Node(node.BorderLeft![node.BorderLeft!.Count - 1]);
                var r = g.Node(node.BorderRight![node.BorderRight!.Count - 1]);

                node.Width = Math.Abs(r.X!.Value - l.X!.Value);
                node.Height = Math.Abs(b.Y!.Value - t.Y!.Value);
                node.X = l.X!.Value + node.Width / 2;
                node.Y = t.Y!.Value + node.Height / 2;
            }
        }

        foreach (var v in g.Nodes())
        {
            if (g.Node(v).Dummy == "border")
            {
                g.RemoveNode(v);
            }
        }
    }

    static void RemoveSelfEdges(Graph g)
    {
        foreach (var e in g.Edges())
        {
            if (e.V == e.W)
            {
                var node = g.Node(e.V);
                node.SelfEdges ??= [];
                node.SelfEdges.Add(new() { E = e, Label = g.FindEdgeLabel(e) });
                g.RemoveEdge(e);
            }
        }
    }

    static void InsertSelfEdges(Graph g)
    {
        var layers = Util.BuildLayerMatrix(g);
        foreach (var layer in layers)
        {
            var orderShift = 0;
            for (var i = 0; i < layer.Count; i++)
            {
                var v = layer[i];
                var node = g.Node(v);
                node.Order = i + orderShift;
                foreach (var selfEdge in node.SelfEdges ?? [])
                {
                    Util.AddDummyNode(g, "selfedge", new()
                    {
                        Width = selfEdge.Label.Width!.Value,
                        Height = selfEdge.Label.Height!.Value,
                        Rank = node.Rank,
                        Order = i + (++orderShift),
                        EdgeObj = selfEdge.E,
                        EdgeLabel = selfEdge.Label
                    }, "_se");
                }

                node.SelfEdges = null;
            }
        }
    }

    static void PositionSelfEdges(Graph g)
    {
        foreach (var v in g.Nodes())
        {
            var node = g.Node(v);
            if (node.Dummy == "selfedge")
            {
                var selfNode = g.Node(node.EdgeObj!.V);
                var x = selfNode.X!.Value + selfNode.Width / 2;
                var y = selfNode.Y!.Value;
                var dx = node.X!.Value - x;
                var dy = selfNode.Height / 2;
                var label = node.EdgeLabel!;
                g.SetEdge(node.EdgeObj!, label);
                g.RemoveNode(v);
                label.Points =
                [
                    new(x + 2 * dx / 3, y - dy),
                    new(x + 5 * dx / 6, y - dy),
                    new(x + dx, y),
                    new(x + 5 * dx / 6, y + dy),
                    new(x + 2 * dx / 3, y + dy)
                ];
                label.X = node.X;
                label.Y = node.Y;
            }
        }
    }
}
