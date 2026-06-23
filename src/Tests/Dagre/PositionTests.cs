public class PositionTests
{
    Graph graph = null!;

    [Before(Test)]
    public void Setup()
    {
        graph = new Graph(compound: true);
        graph.SetGraph(new()
        {
            RankSeparation = 50,
            NodeSeparation = 50,
            EdgeSeparation = 10
        });
    }

    [Test]
    public async Task RespectsRankSeparation()
    {
        graph.Label.RankSeparation = 1000;
        graph.SetNode("a", new() { Width = 50, Height = 100, Rank = 0, Order = 0 });
        graph.SetNode("b", new() { Width = 50, Height = 80, Rank = 1, Order = 0 });
        graph.SetEdge("a", "b");
        Positioning.Run(graph);
        await Assert.That(graph.NodeLabel("b").Y!.Value).IsEqualTo(100 + 1000 + 80 / 2.0);
    }

    [Test]
    public async Task UseTheLargestHeightInEachRankWithRankSeparation()
    {
        graph.Label.RankSeparation = 1000;
        graph.SetNode("a", new() { Width = 50, Height = 100, Rank = 0, Order = 0 });
        graph.SetNode("b", new() { Width = 50, Height = 80, Rank = 0, Order = 1 });
        graph.SetNode("c", new() { Width = 50, Height = 90, Rank = 1, Order = 0 });
        graph.SetEdge("a", "c");
        Positioning.Run(graph);
        await Assert.That(graph.NodeLabel("a").Y!.Value).IsEqualTo(100 / 2.0);
        await Assert.That(graph.NodeLabel("b").Y!.Value).IsEqualTo(100 / 2.0); // Note we used 100 and not 80 here
        await Assert.That(graph.NodeLabel("c").Y!.Value).IsEqualTo(100 + 1000 + 90 / 2.0);
    }

    [Test]
    public async Task RespectsNodeSeparation()
    {
        graph.Label.NodeSeparation = 1000;
        graph.SetNode("a", new() { Width = 50, Height = 100, Rank = 0, Order = 0 });
        graph.SetNode("b", new() { Width = 70, Height = 80, Rank = 0, Order = 1 });
        Positioning.Run(graph);
        await Assert.That(graph.NodeLabel("b").X!.Value).IsEqualTo(graph.NodeLabel("a").X!.Value + 50 / 2.0 + 1000 + 70 / 2.0);
    }

    [Test]
    public async Task ShouldNotTryToPositionTheSubgraphNodeItself()
    {
        graph.SetNode("a", new() { Width = 50, Height = 50, Rank = 0, Order = 0 });
        graph.SetNode("sg1", new());
        graph.SetParent("a", "sg1");
        Positioning.Run(graph);
        await Assert.That(graph.NodeLabel("sg1").X).IsNull();
        await Assert.That(graph.NodeLabel("sg1").Y).IsNull();
    }

    [Test]
    public async Task AlignsNodesToCenterOfRank()
    {
        graph.SetNode("a", new() { Width = 50, Height = 100, Rank = 0, Order = 0 });
        graph.SetNode("b", new() { Width = 50, Height = 60, Rank = 0, Order = 1 });
        Positioning.Run(graph);
        await Assert.That(graph.NodeLabel("a").Y!.Value).IsEqualTo(100 / 2.0);
        await Assert.That(graph.NodeLabel("b").Y!.Value).IsEqualTo(100 / 2.0);
    }
}
