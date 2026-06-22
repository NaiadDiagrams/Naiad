namespace Naiad.Dagre.Tests;

public class SmokeTest
{
    static Graph NewGraph() =>
        new Graph(directed: true, multigraph: true, compound: true)
            .SetGraph(new())
            .SetDefaultEdgeLabel(new EdgeLabel());

    [Test]
    public async Task LaysOutASimpleChainTopToBottom()
    {
        var g = NewGraph();
        g.SetNode("a", new() { Width = 50, Height = 50 });
        g.SetNode("b", new() { Width = 50, Height = 50 });
        g.SetNode("c", new() { Width = 50, Height = 50 });
        g.SetEdge("a", "b");
        g.SetEdge("b", "c");

        Layout.Run(g);

        var a = g.NodeLabel("a");
        var b = g.NodeLabel("b");
        var c = g.NodeLabel("c");

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
            g.SetNode(v, new() { Width = 40, Height = 40 });
        }

        g.SetEdge("a", "b");
        g.SetEdge("a", "c");
        g.SetEdge("b", "d");
        g.SetEdge("c", "d");

        Layout.Run(g);

        // b and c are siblings on the same rank; a above, d below.
        await Assert.That(g.NodeLabel("b").Y!.Value).IsEqualTo(g.NodeLabel("c").Y!.Value).Within(0.001);
        await Assert.That(g.NodeLabel("a").Y!.Value).IsLessThan(g.NodeLabel("b").Y!.Value);
        await Assert.That(g.NodeLabel("d").Y!.Value).IsGreaterThan(g.NodeLabel("b").Y!.Value);
    }
}
