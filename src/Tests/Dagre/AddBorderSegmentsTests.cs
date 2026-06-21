namespace Naiad.Dagre.Tests;

public class AddBorderSegmentsTests
{
    Graph g = null!;

    [Before(Test)]
    public void Setup() => g = new(compound: true);

    [Test]
    public async Task DoesNotAddBorderNodesForANonCompoundGraph()
    {
        var graph = new Graph();
        graph.SetNode("a", new() { Rank = 0 });
        AddBorderSegments.Run(graph);
        await Assert.That(graph.NodeCount).IsEqualTo(1);

        var a = graph.Node("a");
        await Assert.That(a.Rank).IsEqualTo(0);
        await Assert.That(a.BorderLeft).IsNull();
        await Assert.That(a.BorderRight).IsNull();
        await Assert.That(a.Dummy).IsNull();
    }

    [Test]
    public async Task DoesNotAddBorderNodesForAGraphWithNoClusters()
    {
        g.SetNode("a", new() { Rank = 0 });
        AddBorderSegments.Run(g);
        await Assert.That(g.NodeCount).IsEqualTo(1);

        var a = g.Node("a");
        await Assert.That(a.Rank).IsEqualTo(0);
        await Assert.That(a.BorderLeft).IsNull();
        await Assert.That(a.BorderRight).IsNull();
        await Assert.That(a.Dummy).IsNull();
    }

    [Test]
    public async Task AddsABorderForASingleRankSubgraph()
    {
        g.SetNode("sg", new() { MinRank = 1, MaxRank = 1 });
        AddBorderSegments.Run(g);

        var bl = g.Node("sg").BorderLeft![1];
        var br = g.Node("sg").BorderRight![1];

        var blNode = g.Node(bl);
        await Assert.That(blNode.Dummy).IsEqualTo("border");
        await Assert.That(blNode.BorderType).IsEqualTo("borderLeft");
        await Assert.That(blNode.Rank).IsEqualTo(1);
        await Assert.That(blNode.Width).IsEqualTo(0);
        await Assert.That(blNode.Height).IsEqualTo(0);
        await Assert.That(g.Parent(bl)).IsEqualTo("sg");

        var brNode = g.Node(br);
        await Assert.That(brNode.Dummy).IsEqualTo("border");
        await Assert.That(brNode.BorderType).IsEqualTo("borderRight");
        await Assert.That(brNode.Rank).IsEqualTo(1);
        await Assert.That(brNode.Width).IsEqualTo(0);
        await Assert.That(brNode.Height).IsEqualTo(0);
        await Assert.That(g.Parent(br)).IsEqualTo("sg");
    }

    [Test]
    public async Task AddsABorderForAMultiRankSubgraph()
    {
        g.SetNode("sg", new() { MinRank = 1, MaxRank = 2 });
        AddBorderSegments.Run(g);

        var sgNode = g.Node("sg");
        var bl2 = sgNode.BorderLeft![1];
        var br2 = sgNode.BorderRight![1];

        var bl2Node = g.Node(bl2);
        await Assert.That(bl2Node.Dummy).IsEqualTo("border");
        await Assert.That(bl2Node.BorderType).IsEqualTo("borderLeft");
        await Assert.That(bl2Node.Rank).IsEqualTo(1);
        await Assert.That(bl2Node.Width).IsEqualTo(0);
        await Assert.That(bl2Node.Height).IsEqualTo(0);
        await Assert.That(g.Parent(bl2)).IsEqualTo("sg");

        var br2Node = g.Node(br2);
        await Assert.That(br2Node.Dummy).IsEqualTo("border");
        await Assert.That(br2Node.BorderType).IsEqualTo("borderRight");
        await Assert.That(br2Node.Rank).IsEqualTo(1);
        await Assert.That(br2Node.Width).IsEqualTo(0);
        await Assert.That(br2Node.Height).IsEqualTo(0);
        await Assert.That(g.Parent(br2)).IsEqualTo("sg");

        var bl1 = sgNode.BorderLeft[2];
        var br1 = sgNode.BorderRight[2];

        var bl1Node = g.Node(bl1);
        await Assert.That(bl1Node.Dummy).IsEqualTo("border");
        await Assert.That(bl1Node.BorderType).IsEqualTo("borderLeft");
        await Assert.That(bl1Node.Rank).IsEqualTo(2);
        await Assert.That(bl1Node.Width).IsEqualTo(0);
        await Assert.That(bl1Node.Height).IsEqualTo(0);
        await Assert.That(g.Parent(bl1)).IsEqualTo("sg");

        var br1Node = g.Node(br1);
        await Assert.That(br1Node.Dummy).IsEqualTo("border");
        await Assert.That(br1Node.BorderType).IsEqualTo("borderRight");
        await Assert.That(br1Node.Rank).IsEqualTo(2);
        await Assert.That(br1Node.Width).IsEqualTo(0);
        await Assert.That(br1Node.Height).IsEqualTo(0);
        await Assert.That(g.Parent(br1)).IsEqualTo("sg");

        await Assert.That(g.HasEdge(sgNode.BorderLeft[1], sgNode.BorderLeft[2])).IsTrue();
        await Assert.That(g.HasEdge(sgNode.BorderRight[1], sgNode.BorderRight[2])).IsTrue();
    }

    [Test]
    public async Task AddsBordersForNestedSubgraphs()
    {
        g.SetNode("sg1", new() { MinRank = 1, MaxRank = 1 });
        g.SetNode("sg2", new() { MinRank = 1, MaxRank = 1 });
        g.SetParent("sg2", "sg1");
        AddBorderSegments.Run(g);

        var bl1 = g.Node("sg1").BorderLeft![1];
        var br1 = g.Node("sg1").BorderRight![1];

        var bl1Node = g.Node(bl1);
        await Assert.That(bl1Node.Dummy).IsEqualTo("border");
        await Assert.That(bl1Node.BorderType).IsEqualTo("borderLeft");
        await Assert.That(bl1Node.Rank).IsEqualTo(1);
        await Assert.That(bl1Node.Width).IsEqualTo(0);
        await Assert.That(bl1Node.Height).IsEqualTo(0);
        await Assert.That(g.Parent(bl1)).IsEqualTo("sg1");

        var br1Node = g.Node(br1);
        await Assert.That(br1Node.Dummy).IsEqualTo("border");
        await Assert.That(br1Node.BorderType).IsEqualTo("borderRight");
        await Assert.That(br1Node.Rank).IsEqualTo(1);
        await Assert.That(br1Node.Width).IsEqualTo(0);
        await Assert.That(br1Node.Height).IsEqualTo(0);
        await Assert.That(g.Parent(br1)).IsEqualTo("sg1");

        var bl2 = g.Node("sg2").BorderLeft![1];
        var br2 = g.Node("sg2").BorderRight![1];

        var bl2Node = g.Node(bl2);
        await Assert.That(bl2Node.Dummy).IsEqualTo("border");
        await Assert.That(bl2Node.BorderType).IsEqualTo("borderLeft");
        await Assert.That(bl2Node.Rank).IsEqualTo(1);
        await Assert.That(bl2Node.Width).IsEqualTo(0);
        await Assert.That(bl2Node.Height).IsEqualTo(0);
        await Assert.That(g.Parent(bl2)).IsEqualTo("sg2");

        var br2Node = g.Node(br2);
        await Assert.That(br2Node.Dummy).IsEqualTo("border");
        await Assert.That(br2Node.BorderType).IsEqualTo("borderRight");
        await Assert.That(br2Node.Rank).IsEqualTo(1);
        await Assert.That(br2Node.Width).IsEqualTo(0);
        await Assert.That(br2Node.Height).IsEqualTo(0);
        await Assert.That(g.Parent(br2)).IsEqualTo("sg2");
    }
}
