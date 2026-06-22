namespace Naiad.Dagre.Tests;

// Ported from .dagre-ref/dagre/test/position-test.ts
public class PositionTests
{
    Graph g = null!;

    [Before(Test)]
    public void Setup()
    {
        g = new Graph(compound: true)
            .SetGraph(new()
            {
                Ranksep = 50,
                Nodesep = 50,
                Edgesep = 10
            });
    }

    [Test]
    public async Task RespectsRanksep()
    {
        g.GraphLabel.Ranksep = 1000;
        g.SetNode("a", new() { Width = 50, Height = 100, Rank = 0, Order = 0 });
        g.SetNode("b", new() { Width = 50, Height = 80, Rank = 1, Order = 0 });
        g.SetEdge("a", "b");
        Position.Run(g);
        await Assert.That(g.Node("b").Y!.Value).IsEqualTo(100 + 1000 + 80 / 2.0);
    }

    [Test]
    public async Task UseTheLargestHeightInEachRankWithRanksep()
    {
        g.GraphLabel.Ranksep = 1000;
        g.SetNode("a", new() { Width = 50, Height = 100, Rank = 0, Order = 0 });
        g.SetNode("b", new() { Width = 50, Height = 80, Rank = 0, Order = 1 });
        g.SetNode("c", new() { Width = 50, Height = 90, Rank = 1, Order = 0 });
        g.SetEdge("a", "c");
        Position.Run(g);
        await Assert.That(g.Node("a").Y!.Value).IsEqualTo(100 / 2.0);
        await Assert.That(g.Node("b").Y!.Value).IsEqualTo(100 / 2.0); // Note we used 100 and not 80 here
        await Assert.That(g.Node("c").Y!.Value).IsEqualTo(100 + 1000 + 90 / 2.0);
    }

    [Test]
    public async Task RespectsNodesep()
    {
        g.GraphLabel.Nodesep = 1000;
        g.SetNode("a", new() { Width = 50, Height = 100, Rank = 0, Order = 0 });
        g.SetNode("b", new() { Width = 70, Height = 80, Rank = 0, Order = 1 });
        Position.Run(g);
        await Assert.That(g.Node("b").X!.Value).IsEqualTo(g.Node("a").X!.Value + 50 / 2.0 + 1000 + 70 / 2.0);
    }

    [Test]
    public async Task ShouldNotTryToPositionTheSubgraphNodeItself()
    {
        g.SetNode("a", new() { Width = 50, Height = 50, Rank = 0, Order = 0 });
        g.SetNode("sg1", new());
        g.SetParent("a", "sg1");
        Position.Run(g);
        await Assert.That(g.Node("sg1").X).IsNull();
        await Assert.That(g.Node("sg1").Y).IsNull();
    }

    [Test]
    public async Task AlignsNodesToTopOfRankWhenRankalignIsTop()
    {
        g.GraphLabel.Rankalign = "top";
        g.SetNode("a", new() { Width = 50, Height = 100, Rank = 0, Order = 0 });
        g.SetNode("b", new() { Width = 50, Height = 60, Rank = 0, Order = 1 });
        Position.Run(g);
        await Assert.That(g.Node("a").Y!.Value).IsEqualTo(100 / 2.0);
        await Assert.That(g.Node("b").Y!.Value).IsEqualTo(60 / 2.0);
    }

    [Test]
    public async Task AlignsNodesToBottomOfRankWhenRankalignIsBottom()
    {
        g.GraphLabel.Rankalign = "bottom";
        g.SetNode("a", new() { Width = 50, Height = 100, Rank = 0, Order = 0 });
        g.SetNode("b", new() { Width = 50, Height = 60, Rank = 0, Order = 1 });
        Position.Run(g);
        await Assert.That(g.Node("a").Y!.Value).IsEqualTo(100 - 100 / 2.0);
        await Assert.That(g.Node("b").Y!.Value).IsEqualTo(100 - 60 / 2.0);
    }

    [Test]
    public async Task AlignsNodesToCenterOfRankWhenRankalignIsCenter()
    {
        g.GraphLabel.Rankalign = "center";
        g.SetNode("a", new() { Width = 50, Height = 100, Rank = 0, Order = 0 });
        g.SetNode("b", new() { Width = 50, Height = 60, Rank = 0, Order = 1 });
        Position.Run(g);
        await Assert.That(g.Node("a").Y!.Value).IsEqualTo(100 / 2.0);
        await Assert.That(g.Node("b").Y!.Value).IsEqualTo(100 / 2.0);
    }
}
