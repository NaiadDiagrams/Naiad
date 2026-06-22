namespace Naiad.Dagre.Tests;

/// <summary>
/// Port of dagre's <c>test/layout-test.ts</c> — the big end-to-end layout test.
/// </summary>
public class LayoutTests
{
    Graph graph = null!;

    [Before(Test)]
    public void Setup() =>
        // new Graph({multigraph: true, compound: true}).setGraph({}).setDefaultEdgeLabel(() => ({}));
        graph = new Graph(directed: true, multigraph: true, compound: true)
            .SetGraph(new())
            .SetDefaultEdgeLabel((_, _, _) => new());

    [Test]
    public async Task CanLayoutASingleNode()
    {
        graph.SetNode("a", new() { Width = 50, Height = 100 });
        Layout.Run(graph);

        await AssertCoordinates(("a", 50.0 / 2, 100.0 / 2));
        await Assert.That(graph.NodeLabel("a").X!.Value).IsEqualTo(50.0 / 2);
        await Assert.That(graph.NodeLabel("a").Y!.Value).IsEqualTo(100.0 / 2);
    }

    [Test]
    public Task CanLayoutTwoNodesOnTheSameRank()
    {
        graph.GraphLabel.Nodesep = 200;
        graph.SetNode("a", new() { Width = 50, Height = 100 });
        graph.SetNode("b", new() { Width = 75, Height = 200 });
        Layout.Run(graph);

        return AssertCoordinates(
            ("a", 50.0 / 2, 200.0 / 2),
            ("b", 50 + 200 + 75.0 / 2, 200.0 / 2));
    }

    [Test]
    public async Task CanLayoutTwoNodesConnectedByAnEdge()
    {
        graph.GraphLabel.Ranksep = 300;
        graph.SetNode("a", new() { Width = 50, Height = 100 });
        graph.SetNode("b", new() { Width = 75, Height = 200 });
        graph.SetEdge("a", "b");
        Layout.Run(graph);

        await AssertCoordinates(
            ("a", 75.0 / 2, 100.0 / 2),
            ("b", 75.0 / 2, 100 + 300 + 200.0 / 2));

        // We should not get x, y coordinates if the edge has no label
        await Assert.That(graph.FindEdgeLabel("a", "b").X).IsNull();
        await Assert.That(graph.FindEdgeLabel("a", "b").Y).IsNull();
    }

    [Test]
    public async Task CanLayoutAnEdgeWithALabel()
    {
        graph.GraphLabel.Ranksep = 300;
        graph.SetNode("a", new() { Width = 50, Height = 100 });
        graph.SetNode("b", new() { Width = 75, Height = 200 });
        graph.SetEdge("a", "b", new() { Width = 60, Height = 70, Labelpos = "c" });
        Layout.Run(graph);

        await AssertCoordinates(
            ("a", 75.0 / 2, 100.0 / 2),
            ("b", 75.0 / 2, 100 + 150 + 70 + 150 + 200.0 / 2));
        await Assert.That(graph.FindEdgeLabel("a", "b").X!.Value).IsEqualTo(75.0 / 2);
        await Assert.That(graph.FindEdgeLabel("a", "b").Y!.Value).IsEqualTo(100 + 150 + 70.0 / 2);
    }

    [Test]
    [Arguments("TB")]
    [Arguments("BT")]
    [Arguments("LR")]
    [Arguments("RL")]
    public async Task CanLayoutAnEdgeWithALongLabel(string rankdir)
    {
        graph.GraphLabel.Nodesep = graph.GraphLabel.Edgesep = 10;
        graph.GraphLabel.Rankdir = rankdir;
        foreach (var v in new[] { "a", "b", "c", "d" })
        {
            graph.SetNode(v, new() { Width = 10, Height = 10 });
        }

        graph.SetEdge("a", "c", new() { Width = 2000, Height = 10, Labelpos = "c" });
        graph.SetEdge("b", "d", new() { Width = 1, Height = 1 });
        Layout.Run(graph);

        double p1X, p2X;
        if (rankdir is "TB" or "BT")
        {
            p1X = graph.FindEdgeLabel("a", "c").X!.Value;
            p2X = graph.FindEdgeLabel("b", "d").X!.Value;
        }
        else
        {
            p1X = graph.NodeLabel("a").X!.Value;
            p2X = graph.NodeLabel("c").X!.Value;
        }

        await Assert.That(Math.Abs(p1X - p2X)).IsGreaterThan(1000);
    }

    [Test]
    [Arguments("TB")]
    [Arguments("BT")]
    [Arguments("LR")]
    [Arguments("RL")]
    public async Task CanApplyAnOffset(string rankdir)
    {
        graph.GraphLabel.Nodesep = graph.GraphLabel.Edgesep = 10;
        graph.GraphLabel.Rankdir = rankdir;
        foreach (var v in new[] { "a", "b", "c", "d" })
        {
            graph.SetNode(v, new() { Width = 10, Height = 10 });
        }

        graph.SetEdge("a", "b", new() { Width = 10, Height = 10, Labelpos = "l", Labeloffset = 1000 });
        graph.SetEdge("c", "d", new() { Width = 10, Height = 10, Labelpos = "r", Labeloffset = 1000 });
        Layout.Run(graph);

        if (rankdir is "TB" or "BT")
        {
            await Assert.That(graph.FindEdgeLabel("a", "b").X!.Value - graph.FindEdgeLabel("a", "b").Points![0].X).IsEqualTo(-1000 - 10.0 / 2);
            await Assert.That(graph.FindEdgeLabel("c", "d").X!.Value - graph.FindEdgeLabel("c", "d").Points![0].X).IsEqualTo(1000 + 10.0 / 2);
        }
        else
        {
            await Assert.That(graph.FindEdgeLabel("a", "b").Y!.Value - graph.FindEdgeLabel("a", "b").Points![0].Y).IsEqualTo(-1000 - 10.0 / 2);
            await Assert.That(graph.FindEdgeLabel("c", "d").Y!.Value - graph.FindEdgeLabel("c", "d").Points![0].Y).IsEqualTo(1000 + 10.0 / 2);
        }
    }

    [Test]
    public async Task CanLayoutALongEdgeWithALabel()
    {
        graph.GraphLabel.Ranksep = 300;
        graph.SetNode("a", new() { Width = 50, Height = 100 });
        graph.SetNode("b", new() { Width = 75, Height = 200 });
        graph.SetEdge("a", "b", new() { Width = 60, Height = 70, Minlen = 2, Labelpos = "c" });
        Layout.Run(graph);

        await Assert.That(graph.FindEdgeLabel("a", "b").X!.Value).IsEqualTo(75.0 / 2);
        await Assert.That(graph.FindEdgeLabel("a", "b").Y!.Value).IsGreaterThan(graph.NodeLabel("a").Y!.Value);
        await Assert.That(graph.FindEdgeLabel("a", "b").Y!.Value).IsLessThan(graph.NodeLabel("b").Y!.Value);
    }

    [Test]
    public async Task CanLayoutOutAShortCycle()
    {
        graph.GraphLabel.Ranksep = 200;
        graph.SetNode("a", new() { Width = 100, Height = 100 });
        graph.SetNode("b", new() { Width = 100, Height = 100 });
        graph.SetEdge("a", "b", new() { Weight = 2 });
        graph.SetEdge("b", "a");
        Layout.Run(graph);

        await AssertCoordinates(
            ("a", 100.0 / 2, 100.0 / 2),
            ("b", 100.0 / 2, 100 + 200 + 100.0 / 2));
        // One arrow should point down, one up
        await Assert.That(graph.FindEdgeLabel("a", "b").Points![1].Y).IsGreaterThan(graph.FindEdgeLabel("a", "b").Points![0].Y);
        await Assert.That(graph.FindEdgeLabel("b", "a").Points![0].Y).IsGreaterThan(graph.FindEdgeLabel("b", "a").Points![1].Y);
    }

    [Test]
    public async Task AddsRectangleIntersectsForEdges()
    {
        graph.GraphLabel.Ranksep = 200;
        graph.SetNode("a", new() { Width = 100, Height = 100 });
        graph.SetNode("b", new() { Width = 100, Height = 100 });
        graph.SetEdge("a", "b");
        Layout.Run(graph);

        var points = graph.FindEdgeLabel("a", "b").Points!;
        await Assert.That(points.Count).IsEqualTo(3);
        await AssertPoints(points,
            (100.0 / 2, 100), // intersect with bottom of a
            (100.0 / 2, 100 + 200.0 / 2), // point for edge label
            (100.0 / 2, 100 + 200)); // intersect with top of b
    }

    [Test]
    public async Task AddsRectangleIntersectsForEdgesSpanningMultipleRanks()
    {
        graph.GraphLabel.Ranksep = 200;
        graph.SetNode("a", new() { Width = 100, Height = 100 });
        graph.SetNode("b", new() { Width = 100, Height = 100 });
        graph.SetEdge("a", "b", new() { Minlen = 2 });
        Layout.Run(graph);

        var points = graph.FindEdgeLabel("a", "b").Points!;
        await Assert.That(points.Count).IsEqualTo(5);
        await AssertPoints(points,
            (100.0 / 2, 100), // intersect with bottom of a
            (100.0 / 2, 100 + 200.0 / 2), // bend #1
            (100.0 / 2, 100 + 400.0 / 2), // point for edge label
            (100.0 / 2, 100 + 600.0 / 2), // bend #2
            (100.0 / 2, 100 + 800.0 / 2)); // intersect with top of b
    }

    [Test]
    [Arguments("TB")]
    [Arguments("BT")]
    [Arguments("LR")]
    [Arguments("RL")]
    public async Task CanLayoutASelfLoop(string rankdir)
    {
        graph.GraphLabel.Edgesep = 75;
        graph.GraphLabel.Rankdir = rankdir;
        graph.SetNode("a", new() { Width = 100, Height = 100 });
        graph.SetEdge("a", "a", new() { Width = 50, Height = 50 });
        Layout.Run(graph);

        var nodeA = graph.NodeLabel("a");
        var points = graph.FindEdgeLabel("a", "a").Points!;
        await Assert.That(points.Count).IsEqualTo(7);
        foreach (var point in points)
        {
            if (rankdir != "LR" && rankdir != "RL")
            {
                await Assert.That(point.X).IsGreaterThan(nodeA.X!.Value);
                await Assert.That(Math.Abs(point.Y - nodeA.Y!.Value)).IsLessThanOrEqualTo(nodeA.Height / 2);
            }
            else
            {
                await Assert.That(point.Y).IsGreaterThan(nodeA.Y!.Value);
                await Assert.That(Math.Abs(point.X - nodeA.X!.Value)).IsLessThanOrEqualTo(nodeA.Width / 2);
            }
        }
    }

    [Test]
    public async Task CanLayoutAGraphWithSubgraphs()
    {
        // To be expanded, this primarily ensures nothing blows up for the moment.
        graph.SetNode("a", new() { Width = 50, Height = 50 });
        graph.SetParent("a", "sg1");
        Layout.Run(graph);
        // No assertions in the original test; passing means no exception thrown.
        await Assert.That(graph.HasNode("a")).IsTrue();
    }

    [Test]
    public async Task MinimizesTheHeightOfSubgraphs()
    {
        foreach (var v in new[] { "a", "b", "c", "d", "x", "y" })
        {
            graph.SetNode(v, new() { Width = 50, Height = 50 });
        }

        graph.SetPath(new List<string> { "a", "b", "c", "d" });
        graph.SetEdge("a", "x", new() { Weight = 100 });
        graph.SetEdge("y", "d", new() { Weight = 100 });
        graph.SetParent("x", "sg");
        graph.SetParent("y", "sg");

        // We did not set up an edge (x, y), and we set up high-weight edges from
        // outside of the subgraph to nodes in the subgraph. This is to try to
        // force nodes x and y to be on different ranks, which we want our ranker
        // to avoid.
        Layout.Run(graph);
        await Assert.That(graph.NodeLabel("x").Y!.Value).IsEqualTo(graph.NodeLabel("y").Y!.Value);
    }

    [Test]
    public async Task MinimizesSeparationBetweenNodesNotAdjacentToSubgraphs()
    {
        foreach (var v in new[] { "a", "b", "c" })
        {
            graph.SetNode(v, new() { Width = 50, Height = 50 });
        }

        graph.SetPath(["a", "b", "c"]);
        graph.SetNode("sg", new());
        graph.SetParent("c", "sg");
        Layout.Run(graph);
        await Assert.That(graph.NodeLabel("b").Y!.Value - graph.NodeLabel("a").Y!.Value).IsEqualTo(100);
    }

    [Test]
    public async Task CanLayoutSubgraphsWithDifferentRankdirs()
    {
        graph.SetNode("a", new() { Width = 50, Height = 50 });
        graph.SetNode("sg", new());
        graph.SetParent("a", "sg");

        async Task Check()
        {
            await Assert.That(graph.NodeLabel("sg").Width).IsGreaterThan(50);
            await Assert.That(graph.NodeLabel("sg").Height).IsGreaterThan(50);
            await Assert.That(graph.NodeLabel("sg").X!.Value).IsGreaterThan(50.0 / 2);
            await Assert.That(graph.NodeLabel("sg").Y!.Value).IsGreaterThan(50.0 / 2);
        }

        foreach (var rankdir in new[] { "tb", "bt", "lr", "rl" })
        {
            graph.GraphLabel.Rankdir = rankdir;
            Layout.Run(graph);
            await Check();
        }
    }

    [Test]
    public async Task AddsDimensionsToTheGraph()
    {
        graph.SetNode("a", new() { Width = 100, Height = 50 });
        Layout.Run(graph);
        await Assert.That(graph.GraphLabel.Width!.Value).IsEqualTo(100);
        await Assert.That(graph.GraphLabel.Height!.Value).IsEqualTo(50);
    }

    // describe("ensures all coordinates are in the bounding box for the graph") -> node
    [Test]
    [Arguments("TB")]
    [Arguments("BT")]
    [Arguments("LR")]
    [Arguments("RL")]
    public async Task BoundingBoxNode(string rankdir)
    {
        graph.GraphLabel.Rankdir = rankdir;
        graph.SetNode("a", new() { Width = 100, Height = 200 });
        Layout.Run(graph);
        await Assert.That(graph.NodeLabel("a").X!.Value).IsEqualTo(100.0 / 2);
        await Assert.That(graph.NodeLabel("a").Y!.Value).IsEqualTo(200.0 / 2);
    }

    // describe("ensures all coordinates are in the bounding box for the graph") -> edge, labelpos = l
    [Test]
    [Arguments("TB")]
    [Arguments("BT")]
    [Arguments("LR")]
    [Arguments("RL")]
    public async Task BoundingBoxEdgeLabelposL(string rankdir)
    {
        graph.GraphLabel.Rankdir = rankdir;
        graph.SetNode("a", new() { Width = 100, Height = 100 });
        graph.SetNode("b", new() { Width = 100, Height = 100 });
        graph.SetEdge("a", "b", new() { Width = 1000, Height = 2000, Labelpos = "l", Labeloffset = 0 });
        Layout.Run(graph);
        if (rankdir is "TB" or "BT")
        {
            await Assert.That(graph.FindEdgeLabel("a", "b").X!.Value).IsEqualTo(1000.0 / 2);
        }
        else
        {
            await Assert.That(graph.FindEdgeLabel("a", "b").Y!.Value).IsEqualTo(2000.0 / 2);
        }
    }

    [Test]
    public Task TreatsAttributesWithCaseInsensitivity()
    {
        // The original test sets `graph.graph().nodeSep = 200` (capital S) and relies on dagre's
        // `canonicalize` step lowercasing keys. The C# port uses a strongly-typed GraphLabel
        // where the only spelling is `Nodesep`, so case-insensitivity is not expressible and the
        // canonical field is set directly.
        graph.GraphLabel.Nodesep = 200;
        graph.SetNode("a", new() { Width = 50, Height = 100 });
        graph.SetNode("b", new() { Width = 75, Height = 200 });
        Layout.Run(graph);

        return AssertCoordinates(
            ("a", 50.0 / 2, 200.0 / 2),
            ("b", 50 + 200 + 75.0 / 2, 200.0 / 2));
    }

    // === helpers =========================================================

    /// <summary>Port of the TS <c>extractCoordinates(graph)</c> helper.</summary>
    static Dictionary<string, (double X, double Y)> ExtractCoordinates(Graph graph)
    {
        var acc = new Dictionary<string, (double X, double Y)>(StringComparer.Ordinal);
        foreach (var v in graph.Nodes())
        {
            var node = graph.NodeLabel(v);
            acc[v] = (node.X!.Value, node.Y!.Value);
        }

        return acc;
    }

    async Task AssertCoordinates(params (string V, double X, double Y)[] expected)
    {
        var actual = ExtractCoordinates(graph);
        await Assert.That(actual.Count).IsEqualTo(expected.Length);
        foreach (var (v, x, y) in expected)
        {
            await Assert.That(actual.ContainsKey(v)).IsTrue();
            await Assert.That(actual[v].X).IsEqualTo(x);
            await Assert.That(actual[v].Y).IsEqualTo(y);
        }
    }

    static async Task AssertPoints(List<Point> actual, params (double X, double Y)[] expected)
    {
        await Assert.That(actual.Count).IsEqualTo(expected.Length);
        for (var i = 0; i < expected.Length; i++)
        {
            await Assert.That(actual[i].X).IsEqualTo(expected[i].X);
            await Assert.That(actual[i].Y).IsEqualTo(expected[i].Y);
        }
    }
}
