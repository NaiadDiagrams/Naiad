public class SmokeTest
{
    static Graph NewGraph()
    {
        var graph = new Graph(directed: true, multigraph: true, compound: true);
        graph.SetGraph(new());
        graph.SetDefaultEdgeLabel(new EdgeLabel());
        return graph;
    }

    [Test]
    public async Task LaysOutASimpleChainTopToBottom()
    {
        var graph = NewGraph();
        graph.SetNode("a", new() { Width = 50, Height = 50 });
        graph.SetNode("b", new() { Width = 50, Height = 50 });
        graph.SetNode("c", new() { Width = 50, Height = 50 });
        graph.SetEdge("a", "b");
        graph.SetEdge("b", "c");

        Layout.Run(graph);

        var a = graph.NodeLabel("a");
        var b = graph.NodeLabel("b");
        var c = graph.NodeLabel("c");

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
        var graph = NewGraph();
        foreach (var v in new[] { "a", "b", "c", "d" })
        {
            graph.SetNode(v, new() { Width = 40, Height = 40 });
        }

        graph.SetEdge("a", "b");
        graph.SetEdge("a", "c");
        graph.SetEdge("b", "d");
        graph.SetEdge("c", "d");

        Layout.Run(graph);

        // b and c are siblings on the same rank; a above, d below.
        await Assert.That(graph.NodeLabel("b").Y!.Value).IsEqualTo(graph.NodeLabel("c").Y!.Value).Within(0.001);
        await Assert.That(graph.NodeLabel("a").Y!.Value).IsLessThan(graph.NodeLabel("b").Y!.Value);
        await Assert.That(graph.NodeLabel("d").Y!.Value).IsGreaterThan(graph.NodeLabel("b").Y!.Value);
    }
}
