namespace Naiad.Dagre.Tests;

/// <summary>
/// Port of dagre's <c>test/layout-test.ts</c> — the big end-to-end layout test.
/// </summary>
public class LayoutTests
{
    Graph g = null!;

    [Before(Test)]
    public void Setup() =>
        // new Graph({multigraph: true, compound: true}).setGraph({}).setDefaultEdgeLabel(() => ({}));
        g = new Graph(directed: true, multigraph: true, compound: true)
            .SetGraph(new GraphLabel())
            .SetDefaultEdgeLabel((_, _, _) => new EdgeLabel());

    [Test]
    public async Task CanLayoutASingleNode()
    {
        g.SetNode("a", new NodeLabel { Width = 50, Height = 100 });
        Layout.Run(g);

        await AssertCoordinates(("a", 50.0 / 2, 100.0 / 2));
        await Assert.That(g.Node("a").X!.Value).IsEqualTo(50.0 / 2);
        await Assert.That(g.Node("a").Y!.Value).IsEqualTo(100.0 / 2);
    }

    [Test]
    public Task CanLayoutTwoNodesOnTheSameRank()
    {
        g.Graph_().Nodesep = 200;
        g.SetNode("a", new NodeLabel { Width = 50, Height = 100 });
        g.SetNode("b", new NodeLabel { Width = 75, Height = 200 });
        Layout.Run(g);

        return AssertCoordinates(
            ("a", 50.0 / 2, 200.0 / 2),
            ("b", 50 + 200 + 75.0 / 2, 200.0 / 2));
    }

    [Test]
    public async Task CanLayoutTwoNodesConnectedByAnEdge()
    {
        g.Graph_().Ranksep = 300;
        g.SetNode("a", new NodeLabel { Width = 50, Height = 100 });
        g.SetNode("b", new NodeLabel { Width = 75, Height = 200 });
        g.SetEdge("a", "b");
        Layout.Run(g);

        await AssertCoordinates(
            ("a", 75.0 / 2, 100.0 / 2),
            ("b", 75.0 / 2, 100 + 300 + 200.0 / 2));

        // We should not get x, y coordinates if the edge has no label
        await Assert.That(g.Edge_("a", "b").X).IsNull();
        await Assert.That(g.Edge_("a", "b").Y).IsNull();
    }

    [Test]
    public async Task CanLayoutAnEdgeWithALabel()
    {
        g.Graph_().Ranksep = 300;
        g.SetNode("a", new NodeLabel { Width = 50, Height = 100 });
        g.SetNode("b", new NodeLabel { Width = 75, Height = 200 });
        g.SetEdge("a", "b", new EdgeLabel { Width = 60, Height = 70, Labelpos = "c" });
        Layout.Run(g);

        await AssertCoordinates(
            ("a", 75.0 / 2, 100.0 / 2),
            ("b", 75.0 / 2, 100 + 150 + 70 + 150 + 200.0 / 2));
        await Assert.That(g.Edge_("a", "b").X!.Value).IsEqualTo(75.0 / 2);
        await Assert.That(g.Edge_("a", "b").Y!.Value).IsEqualTo(100 + 150 + 70.0 / 2);
    }

    [Test]
    [Arguments("TB")]
    [Arguments("BT")]
    [Arguments("LR")]
    [Arguments("RL")]
    public async Task CanLayoutAnEdgeWithALongLabel(string rankdir)
    {
        g.Graph_().Nodesep = g.Graph_().Edgesep = 10;
        g.Graph_().Rankdir = rankdir;
        foreach (var v in new[] { "a", "b", "c", "d" })
        {
            g.SetNode(v, new NodeLabel { Width = 10, Height = 10 });
        }

        g.SetEdge("a", "c", new EdgeLabel { Width = 2000, Height = 10, Labelpos = "c" });
        g.SetEdge("b", "d", new EdgeLabel { Width = 1, Height = 1 });
        Layout.Run(g);

        double p1X, p2X;
        if (rankdir is "TB" or "BT")
        {
            p1X = g.Edge_("a", "c").X!.Value;
            p2X = g.Edge_("b", "d").X!.Value;
        }
        else
        {
            p1X = g.Node("a").X!.Value;
            p2X = g.Node("c").X!.Value;
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
        g.Graph_().Nodesep = g.Graph_().Edgesep = 10;
        g.Graph_().Rankdir = rankdir;
        foreach (var v in new[] { "a", "b", "c", "d" })
        {
            g.SetNode(v, new NodeLabel { Width = 10, Height = 10 });
        }

        g.SetEdge("a", "b", new EdgeLabel { Width = 10, Height = 10, Labelpos = "l", Labeloffset = 1000 });
        g.SetEdge("c", "d", new EdgeLabel { Width = 10, Height = 10, Labelpos = "r", Labeloffset = 1000 });
        Layout.Run(g);

        if (rankdir is "TB" or "BT")
        {
            await Assert.That(g.Edge_("a", "b").X!.Value - g.Edge_("a", "b").Points![0].X).IsEqualTo(-1000 - 10.0 / 2);
            await Assert.That(g.Edge_("c", "d").X!.Value - g.Edge_("c", "d").Points![0].X).IsEqualTo(1000 + 10.0 / 2);
        }
        else
        {
            await Assert.That(g.Edge_("a", "b").Y!.Value - g.Edge_("a", "b").Points![0].Y).IsEqualTo(-1000 - 10.0 / 2);
            await Assert.That(g.Edge_("c", "d").Y!.Value - g.Edge_("c", "d").Points![0].Y).IsEqualTo(1000 + 10.0 / 2);
        }
    }

    [Test]
    public async Task CanLayoutALongEdgeWithALabel()
    {
        g.Graph_().Ranksep = 300;
        g.SetNode("a", new NodeLabel { Width = 50, Height = 100 });
        g.SetNode("b", new NodeLabel { Width = 75, Height = 200 });
        g.SetEdge("a", "b", new EdgeLabel { Width = 60, Height = 70, Minlen = 2, Labelpos = "c" });
        Layout.Run(g);

        await Assert.That(g.Edge_("a", "b").X!.Value).IsEqualTo(75.0 / 2);
        await Assert.That(g.Edge_("a", "b").Y!.Value).IsGreaterThan(g.Node("a").Y!.Value);
        await Assert.That(g.Edge_("a", "b").Y!.Value).IsLessThan(g.Node("b").Y!.Value);
    }

    [Test]
    public async Task CanLayoutOutAShortCycle()
    {
        g.Graph_().Ranksep = 200;
        g.SetNode("a", new NodeLabel { Width = 100, Height = 100 });
        g.SetNode("b", new NodeLabel { Width = 100, Height = 100 });
        g.SetEdge("a", "b", new EdgeLabel { Weight = 2 });
        g.SetEdge("b", "a");
        Layout.Run(g);

        await AssertCoordinates(
            ("a", 100.0 / 2, 100.0 / 2),
            ("b", 100.0 / 2, 100 + 200 + 100.0 / 2));
        // One arrow should point down, one up
        await Assert.That(g.Edge_("a", "b").Points![1].Y).IsGreaterThan(g.Edge_("a", "b").Points![0].Y);
        await Assert.That(g.Edge_("b", "a").Points![0].Y).IsGreaterThan(g.Edge_("b", "a").Points![1].Y);
    }

    [Test]
    public async Task AddsRectangleIntersectsForEdges()
    {
        g.Graph_().Ranksep = 200;
        g.SetNode("a", new NodeLabel { Width = 100, Height = 100 });
        g.SetNode("b", new NodeLabel { Width = 100, Height = 100 });
        g.SetEdge("a", "b");
        Layout.Run(g);

        var points = g.Edge_("a", "b").Points!;
        await Assert.That(points.Count).IsEqualTo(3);
        await AssertPoints(points,
            (100.0 / 2, 100), // intersect with bottom of a
            (100.0 / 2, 100 + 200.0 / 2), // point for edge label
            (100.0 / 2, 100 + 200)); // intersect with top of b
    }

    [Test]
    public async Task AddsRectangleIntersectsForEdgesSpanningMultipleRanks()
    {
        g.Graph_().Ranksep = 200;
        g.SetNode("a", new NodeLabel { Width = 100, Height = 100 });
        g.SetNode("b", new NodeLabel { Width = 100, Height = 100 });
        g.SetEdge("a", "b", new EdgeLabel { Minlen = 2 });
        Layout.Run(g);

        var points = g.Edge_("a", "b").Points!;
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
        g.Graph_().Edgesep = 75;
        g.Graph_().Rankdir = rankdir;
        g.SetNode("a", new NodeLabel { Width = 100, Height = 100 });
        g.SetEdge("a", "a", new EdgeLabel { Width = 50, Height = 50 });
        Layout.Run(g);

        var nodeA = g.Node("a");
        var points = g.Edge_("a", "a").Points!;
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
        g.SetNode("a", new NodeLabel { Width = 50, Height = 50 });
        g.SetParent("a", "sg1");
        Layout.Run(g);
        // No assertions in the original test; passing means no exception thrown.
        await Assert.That(g.HasNode("a")).IsTrue();
    }

    [Test]
    public async Task MinimizesTheHeightOfSubgraphs()
    {
        foreach (var v in new[] { "a", "b", "c", "d", "x", "y" })
        {
            g.SetNode(v, new NodeLabel { Width = 50, Height = 50 });
        }

        g.SetPath(new List<string> { "a", "b", "c", "d" });
        g.SetEdge("a", "x", new EdgeLabel { Weight = 100 });
        g.SetEdge("y", "d", new EdgeLabel { Weight = 100 });
        g.SetParent("x", "sg");
        g.SetParent("y", "sg");

        // We did not set up an edge (x, y), and we set up high-weight edges from
        // outside of the subgraph to nodes in the subgraph. This is to try to
        // force nodes x and y to be on different ranks, which we want our ranker
        // to avoid.
        Layout.Run(g);
        await Assert.That(g.Node("x").Y!.Value).IsEqualTo(g.Node("y").Y!.Value);
    }

    [Test]
    public async Task MinimizesSeparationBetweenNodesNotAdjacentToSubgraphs()
    {
        foreach (var v in new[] { "a", "b", "c" })
        {
            g.SetNode(v, new NodeLabel { Width = 50, Height = 50 });
        }

        g.SetPath(["a", "b", "c"]);
        g.SetNode("sg", new NodeLabel());
        g.SetParent("c", "sg");
        Layout.Run(g);
        await Assert.That(g.Node("b").Y!.Value - g.Node("a").Y!.Value).IsEqualTo(100);
    }

    [Test]
    public async Task CanLayoutSubgraphsWithDifferentRankdirs()
    {
        g.SetNode("a", new NodeLabel { Width = 50, Height = 50 });
        g.SetNode("sg", new NodeLabel());
        g.SetParent("a", "sg");

        async Task Check()
        {
            await Assert.That(g.Node("sg").Width).IsGreaterThan(50);
            await Assert.That(g.Node("sg").Height).IsGreaterThan(50);
            await Assert.That(g.Node("sg").X!.Value).IsGreaterThan(50.0 / 2);
            await Assert.That(g.Node("sg").Y!.Value).IsGreaterThan(50.0 / 2);
        }

        foreach (var rankdir in new[] { "tb", "bt", "lr", "rl" })
        {
            g.Graph_().Rankdir = rankdir;
            Layout.Run(g);
            await Check();
        }
    }

    [Test]
    public async Task AddsDimensionsToTheGraph()
    {
        g.SetNode("a", new NodeLabel { Width = 100, Height = 50 });
        Layout.Run(g);
        await Assert.That(g.Graph_().Width!.Value).IsEqualTo(100);
        await Assert.That(g.Graph_().Height!.Value).IsEqualTo(50);
    }

    // describe("ensures all coordinates are in the bounding box for the graph") -> node
    [Test]
    [Arguments("TB")]
    [Arguments("BT")]
    [Arguments("LR")]
    [Arguments("RL")]
    public async Task BoundingBoxNode(string rankdir)
    {
        g.Graph_().Rankdir = rankdir;
        g.SetNode("a", new NodeLabel { Width = 100, Height = 200 });
        Layout.Run(g);
        await Assert.That(g.Node("a").X!.Value).IsEqualTo(100.0 / 2);
        await Assert.That(g.Node("a").Y!.Value).IsEqualTo(200.0 / 2);
    }

    // describe("ensures all coordinates are in the bounding box for the graph") -> edge, labelpos = l
    [Test]
    [Arguments("TB")]
    [Arguments("BT")]
    [Arguments("LR")]
    [Arguments("RL")]
    public async Task BoundingBoxEdgeLabelposL(string rankdir)
    {
        g.Graph_().Rankdir = rankdir;
        g.SetNode("a", new NodeLabel { Width = 100, Height = 100 });
        g.SetNode("b", new NodeLabel { Width = 100, Height = 100 });
        g.SetEdge("a", "b", new EdgeLabel { Width = 1000, Height = 2000, Labelpos = "l", Labeloffset = 0 });
        Layout.Run(g);
        if (rankdir is "TB" or "BT")
        {
            await Assert.That(g.Edge_("a", "b").X!.Value).IsEqualTo(1000.0 / 2);
        }
        else
        {
            await Assert.That(g.Edge_("a", "b").Y!.Value).IsEqualTo(2000.0 / 2);
        }
    }

    [Test]
    public Task TreatsAttributesWithCaseInsensitivity()
    {
        // The original test sets `g.graph().nodeSep = 200` (capital S) and relies on dagre's
        // `canonicalize` step lowercasing keys. The C# port uses a strongly-typed GraphLabel
        // where the only spelling is `Nodesep`, so case-insensitivity is not expressible and the
        // canonical field is set directly.
        g.Graph_().Nodesep = 200;
        g.SetNode("a", new NodeLabel { Width = 50, Height = 100 });
        g.SetNode("b", new NodeLabel { Width = 75, Height = 200 });
        Layout.Run(g);

        return AssertCoordinates(
            ("a", 50.0 / 2, 200.0 / 2),
            ("b", 50 + 200 + 75.0 / 2, 200.0 / 2));
    }

    // === helpers =========================================================

    /// <summary>Port of the TS <c>extractCoordinates(g)</c> helper.</summary>
    static Dictionary<string, (double X, double Y)> ExtractCoordinates(Graph g)
    {
        var acc = new Dictionary<string, (double X, double Y)>(StringComparer.Ordinal);
        foreach (var v in g.Nodes())
        {
            var node = g.Node(v);
            acc[v] = (node.X!.Value, node.Y!.Value);
        }

        return acc;
    }

    async Task AssertCoordinates(params (string V, double X, double Y)[] expected)
    {
        var actual = ExtractCoordinates(g);
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
