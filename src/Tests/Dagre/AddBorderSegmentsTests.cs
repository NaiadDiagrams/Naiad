public class AddBorderSegmentsTests
{
    Graph graph = null!;

    [Before(Test)]
    public void Setup() => graph = new(compound: true);

    [Test]
    public async Task DoesNotAddBorderNodesForANonCompoundGraph()
    {
        var graph = new Graph();
        graph.SetNode("a", new() { Rank = 0 });
        AddBorderSegments.Run(graph);
        await Assert.That(graph.NodeCount).IsEqualTo(1);

        var a = graph.NodeLabel("a");
        await Assert.That(a.Rank).IsEqualTo(0);
        await Assert.That(a.BorderLeft).IsNull();
        await Assert.That(a.BorderRight).IsNull();
        await Assert.That(a.Dummy).IsNull();
    }

    [Test]
    public async Task DoesNotAddBorderNodesForAGraphWithNoClusters()
    {
        graph.SetNode("a", new() { Rank = 0 });
        AddBorderSegments.Run(graph);
        await Assert.That(graph.NodeCount).IsEqualTo(1);

        var a = graph.NodeLabel("a");
        await Assert.That(a.Rank).IsEqualTo(0);
        await Assert.That(a.BorderLeft).IsNull();
        await Assert.That(a.BorderRight).IsNull();
        await Assert.That(a.Dummy).IsNull();
    }

    [Test]
    public async Task AddsABorderForASingleRankSubgraph()
    {
        graph.SetNode("sg", new() { MinRank = 1, MaxRank = 1 });
        AddBorderSegments.Run(graph);

        var bl = graph.NodeLabel("sg").BorderLeft![1];
        var br = graph.NodeLabel("sg").BorderRight![1];

        var blNode = graph.NodeLabel(bl);
        await Assert.That(blNode.Dummy).IsEqualTo("border");
        await Assert.That(blNode.BorderType).IsEqualTo("borderLeft");
        await Assert.That(blNode.Rank).IsEqualTo(1);
        await Assert.That(blNode.Width).IsEqualTo(0);
        await Assert.That(blNode.Height).IsEqualTo(0);
        await Assert.That(graph.Parent(bl)).IsEqualTo("sg");

        var brNode = graph.NodeLabel(br);
        await Assert.That(brNode.Dummy).IsEqualTo("border");
        await Assert.That(brNode.BorderType).IsEqualTo("borderRight");
        await Assert.That(brNode.Rank).IsEqualTo(1);
        await Assert.That(brNode.Width).IsEqualTo(0);
        await Assert.That(brNode.Height).IsEqualTo(0);
        await Assert.That(graph.Parent(br)).IsEqualTo("sg");
    }

    [Test]
    public async Task AddsABorderForAMultiRankSubgraph()
    {
        graph.SetNode("sg", new() { MinRank = 1, MaxRank = 2 });
        AddBorderSegments.Run(graph);

        var sgNode = graph.NodeLabel("sg");
        var bl2 = sgNode.BorderLeft![1];
        var br2 = sgNode.BorderRight![1];

        var bl2Node = graph.NodeLabel(bl2);
        await Assert.That(bl2Node.Dummy).IsEqualTo("border");
        await Assert.That(bl2Node.BorderType).IsEqualTo("borderLeft");
        await Assert.That(bl2Node.Rank).IsEqualTo(1);
        await Assert.That(bl2Node.Width).IsEqualTo(0);
        await Assert.That(bl2Node.Height).IsEqualTo(0);
        await Assert.That(graph.Parent(bl2)).IsEqualTo("sg");

        var br2Node = graph.NodeLabel(br2);
        await Assert.That(br2Node.Dummy).IsEqualTo("border");
        await Assert.That(br2Node.BorderType).IsEqualTo("borderRight");
        await Assert.That(br2Node.Rank).IsEqualTo(1);
        await Assert.That(br2Node.Width).IsEqualTo(0);
        await Assert.That(br2Node.Height).IsEqualTo(0);
        await Assert.That(graph.Parent(br2)).IsEqualTo("sg");

        var bl1 = sgNode.BorderLeft[2];
        var br1 = sgNode.BorderRight[2];

        var bl1Node = graph.NodeLabel(bl1);
        await Assert.That(bl1Node.Dummy).IsEqualTo("border");
        await Assert.That(bl1Node.BorderType).IsEqualTo("borderLeft");
        await Assert.That(bl1Node.Rank).IsEqualTo(2);
        await Assert.That(bl1Node.Width).IsEqualTo(0);
        await Assert.That(bl1Node.Height).IsEqualTo(0);
        await Assert.That(graph.Parent(bl1)).IsEqualTo("sg");

        var br1Node = graph.NodeLabel(br1);
        await Assert.That(br1Node.Dummy).IsEqualTo("border");
        await Assert.That(br1Node.BorderType).IsEqualTo("borderRight");
        await Assert.That(br1Node.Rank).IsEqualTo(2);
        await Assert.That(br1Node.Width).IsEqualTo(0);
        await Assert.That(br1Node.Height).IsEqualTo(0);
        await Assert.That(graph.Parent(br1)).IsEqualTo("sg");

        await Assert.That(graph.HasEdge(sgNode.BorderLeft[1], sgNode.BorderLeft[2])).IsTrue();
        await Assert.That(graph.HasEdge(sgNode.BorderRight[1], sgNode.BorderRight[2])).IsTrue();
    }

    [Test]
    public async Task AddsBordersForNestedSubgraphs()
    {
        graph.SetNode("sg1", new() { MinRank = 1, MaxRank = 1 });
        graph.SetNode("sg2", new() { MinRank = 1, MaxRank = 1 });
        graph.SetParent("sg2", "sg1");
        AddBorderSegments.Run(graph);

        var bl1 = graph.NodeLabel("sg1").BorderLeft![1];
        var br1 = graph.NodeLabel("sg1").BorderRight![1];

        var bl1Node = graph.NodeLabel(bl1);
        await Assert.That(bl1Node.Dummy).IsEqualTo("border");
        await Assert.That(bl1Node.BorderType).IsEqualTo("borderLeft");
        await Assert.That(bl1Node.Rank).IsEqualTo(1);
        await Assert.That(bl1Node.Width).IsEqualTo(0);
        await Assert.That(bl1Node.Height).IsEqualTo(0);
        await Assert.That(graph.Parent(bl1)).IsEqualTo("sg1");

        var br1Node = graph.NodeLabel(br1);
        await Assert.That(br1Node.Dummy).IsEqualTo("border");
        await Assert.That(br1Node.BorderType).IsEqualTo("borderRight");
        await Assert.That(br1Node.Rank).IsEqualTo(1);
        await Assert.That(br1Node.Width).IsEqualTo(0);
        await Assert.That(br1Node.Height).IsEqualTo(0);
        await Assert.That(graph.Parent(br1)).IsEqualTo("sg1");

        var bl2 = graph.NodeLabel("sg2").BorderLeft![1];
        var br2 = graph.NodeLabel("sg2").BorderRight![1];

        var bl2Node = graph.NodeLabel(bl2);
        await Assert.That(bl2Node.Dummy).IsEqualTo("border");
        await Assert.That(bl2Node.BorderType).IsEqualTo("borderLeft");
        await Assert.That(bl2Node.Rank).IsEqualTo(1);
        await Assert.That(bl2Node.Width).IsEqualTo(0);
        await Assert.That(bl2Node.Height).IsEqualTo(0);
        await Assert.That(graph.Parent(bl2)).IsEqualTo("sg2");

        var br2Node = graph.NodeLabel(br2);
        await Assert.That(br2Node.Dummy).IsEqualTo("border");
        await Assert.That(br2Node.BorderType).IsEqualTo("borderRight");
        await Assert.That(br2Node.Rank).IsEqualTo(1);
        await Assert.That(br2Node.Width).IsEqualTo(0);
        await Assert.That(br2Node.Height).IsEqualTo(0);
        await Assert.That(graph.Parent(br2)).IsEqualTo("sg2");
    }
}
