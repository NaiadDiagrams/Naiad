namespace Naiad.Dagre.Tests;

// Port of dagre test/rank/util-test.ts (describe "rank/util" > "longestPath").
public class RankUtilTests
{
    Graph graph = null!;

    [Before(Test)]
    public void Setup() =>
        graph = new Graph()
            .SetDefaultNodeLabel(_ => new())
            .SetDefaultEdgeLabel((_, _, _) => new() { Minlen = 1 });

    [Test]
    public async Task CanAssignARankToASingleNodeGraph()
    {
        graph.SetNode("a");
        RankUtil.LongestPath(graph);
        Util.NormalizeRanks(graph);
        await Assert.That(graph.NodeLabel("a").Rank).IsEqualTo(0);
    }

    [Test]
    public async Task CanAssignRanksToUnconnectedNodes()
    {
        graph.SetNode("a");
        graph.SetNode("b");
        RankUtil.LongestPath(graph);
        Util.NormalizeRanks(graph);
        await Assert.That(graph.NodeLabel("a").Rank).IsEqualTo(0);
        await Assert.That(graph.NodeLabel("b").Rank).IsEqualTo(0);
    }

    [Test]
    public async Task CanAssignRanksToConnectedNodes()
    {
        graph.SetEdge("a", "b");
        RankUtil.LongestPath(graph);
        Util.NormalizeRanks(graph);
        await Assert.That(graph.NodeLabel("a").Rank).IsEqualTo(0);
        await Assert.That(graph.NodeLabel("b").Rank).IsEqualTo(1);
    }

    [Test]
    public async Task CanAssignRanksForADiamond()
    {
        graph.SetPath(["a", "b", "d"]);
        graph.SetPath(["a", "c", "d"]);
        RankUtil.LongestPath(graph);
        Util.NormalizeRanks(graph);
        await Assert.That(graph.NodeLabel("a").Rank).IsEqualTo(0);
        await Assert.That(graph.NodeLabel("b").Rank).IsEqualTo(1);
        await Assert.That(graph.NodeLabel("c").Rank).IsEqualTo(1);
        await Assert.That(graph.NodeLabel("d").Rank).IsEqualTo(2);
    }

    [Test]
    public async Task UsesTheMinlenAttributeOnTheEdge()
    {
        graph.SetPath(["a", "b", "d"]);
        graph.SetEdge("a", "c");
        graph.SetEdge("c", "d", new() { Minlen = 2 });
        RankUtil.LongestPath(graph);
        Util.NormalizeRanks(graph);
        await Assert.That(graph.NodeLabel("a").Rank).IsEqualTo(0);
        // longest path biases towards the lowest rank it can assign
        await Assert.That(graph.NodeLabel("b").Rank).IsEqualTo(2);
        await Assert.That(graph.NodeLabel("c").Rank).IsEqualTo(1);
        await Assert.That(graph.NodeLabel("d").Rank).IsEqualTo(3);
    }
}
