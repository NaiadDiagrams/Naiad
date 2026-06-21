namespace Naiad.Dagre.Tests;

public class ParentDummyChainsTests
{
    Graph g = null!;

    [Before(Test)]
    public void Setup() =>
        g = new Graph(compound: true).SetGraph(new());

    [Test]
    public async Task DoesNotSetAParentIfBothTheTailAndHeadHaveNoParent()
    {
        g.SetNode("a");
        g.SetNode("b");
        g.SetNode("d1", new() { EdgeObj = new("a", "b") });
        g.Graph_().DummyChains = ["d1"];
        g.SetPath(["a", "d1", "b"]);

        ParentDummyChains.Run(g);
        await Assert.That(g.Parent("d1")).IsNull();
    }

    [Test]
    public async Task UsesTheTailsParentForTheFirstNodeIfItIsNotTheRoot()
    {
        g.SetParent("a", "sg1");
        g.SetNode("sg1", new() { MinRank = 0, MaxRank = 2 });
        g.SetNode("d1", new() { EdgeObj = new("a", "b"), Rank = 2 });
        g.Graph_().DummyChains = ["d1"];
        g.SetPath(["a", "d1", "b"]);

        ParentDummyChains.Run(g);
        await Assert.That(g.Parent("d1")).IsEqualTo("sg1");
    }

    [Test]
    public async Task UsesTheHeadsParentForTheFirstNodeIfTailsIsRoot()
    {
        g.SetParent("b", "sg1");
        g.SetNode("sg1", new() { MinRank = 1, MaxRank = 3 });
        g.SetNode("d1", new() { EdgeObj = new("a", "b"), Rank = 1 });
        g.Graph_().DummyChains = ["d1"];
        g.SetPath(["a", "d1", "b"]);

        ParentDummyChains.Run(g);
        await Assert.That(g.Parent("d1")).IsEqualTo("sg1");
    }

    [Test]
    public async Task HandlesALongChainStartingInASubgraph()
    {
        g.SetParent("a", "sg1");
        g.SetNode("sg1", new() { MinRank = 0, MaxRank = 2 });
        g.SetNode("d1", new() { EdgeObj = new("a", "b"), Rank = 2 });
        g.SetNode("d2", new() { Rank = 3 });
        g.SetNode("d3", new() { Rank = 4 });
        g.Graph_().DummyChains = ["d1"];
        g.SetPath(["a", "d1", "d2", "d3", "b"]);

        ParentDummyChains.Run(g);
        await Assert.That(g.Parent("d1")).IsEqualTo("sg1");
        await Assert.That(g.Parent("d2")).IsNull();
        await Assert.That(g.Parent("d3")).IsNull();
    }

    [Test]
    public async Task HandlesALongChainEndingInASubgraph()
    {
        g.SetParent("b", "sg1");
        g.SetNode("sg1", new() { MinRank = 3, MaxRank = 5 });
        g.SetNode("d1", new() { EdgeObj = new("a", "b"), Rank = 1 });
        g.SetNode("d2", new() { Rank = 2 });
        g.SetNode("d3", new() { Rank = 3 });
        g.Graph_().DummyChains = ["d1"];
        g.SetPath(["a", "d1", "d2", "d3", "b"]);

        ParentDummyChains.Run(g);
        await Assert.That(g.Parent("d1")).IsNull();
        await Assert.That(g.Parent("d2")).IsNull();
        await Assert.That(g.Parent("d3")).IsEqualTo("sg1");
    }

    [Test]
    public async Task HandlesNestedSubgraphs()
    {
        g.SetParent("a", "sg2");
        g.SetParent("sg2", "sg1");
        g.SetNode("sg1", new() { MinRank = 0, MaxRank = 4 });
        g.SetNode("sg2", new() { MinRank = 1, MaxRank = 3 });
        g.SetParent("b", "sg4");
        g.SetParent("sg4", "sg3");
        g.SetNode("sg3", new() { MinRank = 6, MaxRank = 10 });
        g.SetNode("sg4", new() { MinRank = 7, MaxRank = 9 });
        for (var i = 0; i < 5; ++i)
        {
            g.SetNode("d" + (i + 1), new() { Rank = i + 3 });
        }

        g.Node("d1").EdgeObj = new("a", "b");
        g.Graph_().DummyChains = ["d1"];
        g.SetPath(["a", "d1", "d2", "d3", "d4", "d5", "b"]);

        ParentDummyChains.Run(g);
        await Assert.That(g.Parent("d1")).IsEqualTo("sg2");
        await Assert.That(g.Parent("d2")).IsEqualTo("sg1");
        await Assert.That(g.Parent("d3")).IsNull();
        await Assert.That(g.Parent("d4")).IsEqualTo("sg3");
        await Assert.That(g.Parent("d5")).IsEqualTo("sg4");
    }

    [Test]
    public async Task HandlesOverlappingRankRanges()
    {
        g.SetParent("a", "sg1");
        g.SetNode("sg1", new() { MinRank = 0, MaxRank = 3 });
        g.SetParent("b", "sg2");
        g.SetNode("sg2", new() { MinRank = 2, MaxRank = 6 });
        g.SetNode("d1", new() { EdgeObj = new("a", "b"), Rank = 2 });
        g.SetNode("d2", new() { Rank = 3 });
        g.SetNode("d3", new() { Rank = 4 });
        g.Graph_().DummyChains = ["d1"];
        g.SetPath(["a", "d1", "d2", "d3", "b"]);

        ParentDummyChains.Run(g);
        await Assert.That(g.Parent("d1")).IsEqualTo("sg1");
        await Assert.That(g.Parent("d2")).IsEqualTo("sg1");
        await Assert.That(g.Parent("d3")).IsEqualTo("sg2");
    }

    [Test]
    public async Task HandlesAnLcaThatIsNotTheRootOfTheGraph1()
    {
        g.SetParent("a", "sg1");
        g.SetParent("sg2", "sg1");
        g.SetNode("sg1", new() { MinRank = 0, MaxRank = 6 });
        g.SetParent("b", "sg2");
        g.SetNode("sg2", new() { MinRank = 3, MaxRank = 5 });
        g.SetNode("d1", new() { EdgeObj = new("a", "b"), Rank = 2 });
        g.SetNode("d2", new() { Rank = 3 });
        g.Graph_().DummyChains = ["d1"];
        g.SetPath(["a", "d1", "d2", "b"]);

        ParentDummyChains.Run(g);
        await Assert.That(g.Parent("d1")).IsEqualTo("sg1");
        await Assert.That(g.Parent("d2")).IsEqualTo("sg2");
    }

    [Test]
    public async Task HandlesAnLcaThatIsNotTheRootOfTheGraph2()
    {
        g.SetParent("a", "sg2");
        g.SetParent("sg2", "sg1");
        g.SetNode("sg1", new() { MinRank = 0, MaxRank = 6 });
        g.SetParent("b", "sg1");
        g.SetNode("sg2", new() { MinRank = 1, MaxRank = 3 });
        g.SetNode("d1", new() { EdgeObj = new("a", "b"), Rank = 3 });
        g.SetNode("d2", new() { Rank = 4 });
        g.Graph_().DummyChains = ["d1"];
        g.SetPath(["a", "d1", "d2", "b"]);

        ParentDummyChains.Run(g);
        await Assert.That(g.Parent("d1")).IsEqualTo("sg2");
        await Assert.That(g.Parent("d2")).IsEqualTo("sg1");
    }
}
