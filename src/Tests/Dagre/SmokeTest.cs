namespace Naiad.Dagre.Tests;

public class SmokeTest
{
    static Graph NewGraph() =>
        new Graph(directed: true, multigraph: true, compound: true)
            .SetGraph(new GraphLabel())
            .SetDefaultEdgeLabel(new EdgeLabel());

    [Test]
    public async Task LaysOutASimpleChainTopToBottom()
    {
        var g = NewGraph();
        g.SetNode("a", new NodeLabel { Width = 50, Height = 50 });
        g.SetNode("b", new NodeLabel { Width = 50, Height = 50 });
        g.SetNode("c", new NodeLabel { Width = 50, Height = 50 });
        g.SetEdge("a", "b");
        g.SetEdge("b", "c");

        Layout.Run(g);

        var a = g.Node("a");
        var b = g.Node("b");
        var c = g.Node("c");

        await Assert.That(a.X).IsNotNull();
        await Assert.That(a.Y).IsNotNull();
        // Top-to-bottom: ranks increase downward.
        await Assert.That(b.Y!.Value).IsGreaterThan(a.Y!.Value);
        await Assert.That(c.Y!.Value).IsGreaterThan(b.Y!.Value);
        // The chain should be vertically aligned.
        await Assert.That(a.X!.Value).IsEqualTo(b.X!.Value).Within(0.001);
        await Assert.That(b.X!.Value).IsEqualTo(c.X!.Value).Within(0.001);
    }

    [Test]
    public async Task RanksABranch()
    {
        var g = NewGraph();
        foreach (var v in new[] { "a", "b", "c", "d" })
        {
            g.SetNode(v, new NodeLabel { Width = 40, Height = 40 });
        }

        g.SetEdge("a", "b");
        g.SetEdge("a", "c");
        g.SetEdge("b", "d");
        g.SetEdge("c", "d");

        Layout.Run(g);

        // b and c are siblings on the same rank; a above, d below.
        await Assert.That(g.Node("b").Y!.Value).IsEqualTo(g.Node("c").Y!.Value).Within(0.001);
        await Assert.That(g.Node("a").Y!.Value).IsLessThan(g.Node("b").Y!.Value);
        await Assert.That(g.Node("d").Y!.Value).IsGreaterThan(g.Node("b").Y!.Value);
    }
}
