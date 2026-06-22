public class ParentDummyChainsTests
{
    Graph graph = null!;

    [Before(Test)]
    public void Setup() =>
        graph = new Graph(compound: true).SetGraph(new());

    [Test]
    public async Task DoesNotSetAParentIfBothTheTailAndHeadHaveNoParent()
    {
        graph.SetNode("a");
        graph.SetNode("b");
        graph.SetNode("d1", new() { EdgeObj = new("a", "b") });
        graph.Label.DummyChains = ["d1"];
        graph.SetPath(["a", "d1", "b"]);

        ParentDummyChains.Run(graph);
        await Assert.That(graph.Parent("d1")).IsNull();
    }

    [Test]
    public async Task UsesTheTailsParentForTheFirstNodeIfItIsNotTheRoot()
    {
        graph.SetParent("a", "sg1");
        graph.SetNode("sg1", new() { MinRank = 0, MaxRank = 2 });
        graph.SetNode("d1", new() { EdgeObj = new("a", "b"), Rank = 2 });
        graph.Label.DummyChains = ["d1"];
        graph.SetPath(["a", "d1", "b"]);

        ParentDummyChains.Run(graph);
        await Assert.That(graph.Parent("d1")).IsEqualTo("sg1");
    }

    [Test]
    public async Task UsesTheHeadsParentForTheFirstNodeIfTailsIsRoot()
    {
        graph.SetParent("b", "sg1");
        graph.SetNode("sg1", new() { MinRank = 1, MaxRank = 3 });
        graph.SetNode("d1", new() { EdgeObj = new("a", "b"), Rank = 1 });
        graph.Label.DummyChains = ["d1"];
        graph.SetPath(["a", "d1", "b"]);

        ParentDummyChains.Run(graph);
        await Assert.That(graph.Parent("d1")).IsEqualTo("sg1");
    }

    [Test]
    public async Task HandlesALongChainStartingInASubgraph()
    {
        graph.SetParent("a", "sg1");
        graph.SetNode("sg1", new() { MinRank = 0, MaxRank = 2 });
        graph.SetNode("d1", new() { EdgeObj = new("a", "b"), Rank = 2 });
        graph.SetNode("d2", new() { Rank = 3 });
        graph.SetNode("d3", new() { Rank = 4 });
        graph.Label.DummyChains = ["d1"];
        graph.SetPath(["a", "d1", "d2", "d3", "b"]);

        ParentDummyChains.Run(graph);
        await Assert.That(graph.Parent("d1")).IsEqualTo("sg1");
        await Assert.That(graph.Parent("d2")).IsNull();
        await Assert.That(graph.Parent("d3")).IsNull();
    }

    [Test]
    public async Task HandlesALongChainEndingInASubgraph()
    {
        graph.SetParent("b", "sg1");
        graph.SetNode("sg1", new() { MinRank = 3, MaxRank = 5 });
        graph.SetNode("d1", new() { EdgeObj = new("a", "b"), Rank = 1 });
        graph.SetNode("d2", new() { Rank = 2 });
        graph.SetNode("d3", new() { Rank = 3 });
        graph.Label.DummyChains = ["d1"];
        graph.SetPath(["a", "d1", "d2", "d3", "b"]);

        ParentDummyChains.Run(graph);
        await Assert.That(graph.Parent("d1")).IsNull();
        await Assert.That(graph.Parent("d2")).IsNull();
        await Assert.That(graph.Parent("d3")).IsEqualTo("sg1");
    }

    [Test]
    public async Task HandlesNestedSubgraphs()
    {
        graph.SetParent("a", "sg2");
        graph.SetParent("sg2", "sg1");
        graph.SetNode("sg1", new() { MinRank = 0, MaxRank = 4 });
        graph.SetNode("sg2", new() { MinRank = 1, MaxRank = 3 });
        graph.SetParent("b", "sg4");
        graph.SetParent("sg4", "sg3");
        graph.SetNode("sg3", new() { MinRank = 6, MaxRank = 10 });
        graph.SetNode("sg4", new() { MinRank = 7, MaxRank = 9 });
        for (var i = 0; i < 5; ++i)
        {
            graph.SetNode("d" + (i + 1), new() { Rank = i + 3 });
        }

        graph.NodeLabel("d1").EdgeObj = new("a", "b");
        graph.Label.DummyChains = ["d1"];
        graph.SetPath(["a", "d1", "d2", "d3", "d4", "d5", "b"]);

        ParentDummyChains.Run(graph);
        await Assert.That(graph.Parent("d1")).IsEqualTo("sg2");
        await Assert.That(graph.Parent("d2")).IsEqualTo("sg1");
        await Assert.That(graph.Parent("d3")).IsNull();
        await Assert.That(graph.Parent("d4")).IsEqualTo("sg3");
        await Assert.That(graph.Parent("d5")).IsEqualTo("sg4");
    }

    [Test]
    public async Task HandlesOverlappingRankRanges()
    {
        graph.SetParent("a", "sg1");
        graph.SetNode("sg1", new() { MinRank = 0, MaxRank = 3 });
        graph.SetParent("b", "sg2");
        graph.SetNode("sg2", new() { MinRank = 2, MaxRank = 6 });
        graph.SetNode("d1", new() { EdgeObj = new("a", "b"), Rank = 2 });
        graph.SetNode("d2", new() { Rank = 3 });
        graph.SetNode("d3", new() { Rank = 4 });
        graph.Label.DummyChains = ["d1"];
        graph.SetPath(["a", "d1", "d2", "d3", "b"]);

        ParentDummyChains.Run(graph);
        await Assert.That(graph.Parent("d1")).IsEqualTo("sg1");
        await Assert.That(graph.Parent("d2")).IsEqualTo("sg1");
        await Assert.That(graph.Parent("d3")).IsEqualTo("sg2");
    }

    [Test]
    public async Task HandlesAnLcaThatIsNotTheRootOfTheGraph1()
    {
        graph.SetParent("a", "sg1");
        graph.SetParent("sg2", "sg1");
        graph.SetNode("sg1", new() { MinRank = 0, MaxRank = 6 });
        graph.SetParent("b", "sg2");
        graph.SetNode("sg2", new() { MinRank = 3, MaxRank = 5 });
        graph.SetNode("d1", new() { EdgeObj = new("a", "b"), Rank = 2 });
        graph.SetNode("d2", new() { Rank = 3 });
        graph.Label.DummyChains = ["d1"];
        graph.SetPath(["a", "d1", "d2", "b"]);

        ParentDummyChains.Run(graph);
        await Assert.That(graph.Parent("d1")).IsEqualTo("sg1");
        await Assert.That(graph.Parent("d2")).IsEqualTo("sg2");
    }

    [Test]
    public async Task HandlesAnLcaThatIsNotTheRootOfTheGraph2()
    {
        graph.SetParent("a", "sg2");
        graph.SetParent("sg2", "sg1");
        graph.SetNode("sg1", new() { MinRank = 0, MaxRank = 6 });
        graph.SetParent("b", "sg1");
        graph.SetNode("sg2", new() { MinRank = 1, MaxRank = 3 });
        graph.SetNode("d1", new() { EdgeObj = new("a", "b"), Rank = 3 });
        graph.SetNode("d2", new() { Rank = 4 });
        graph.Label.DummyChains = ["d1"];
        graph.SetPath(["a", "d1", "d2", "b"]);

        ParentDummyChains.Run(graph);
        await Assert.That(graph.Parent("d1")).IsEqualTo("sg2");
        await Assert.That(graph.Parent("d2")).IsEqualTo("sg1");
    }
}
