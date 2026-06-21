namespace Naiad.Dagre.Tests;

// Port of dagre test/rank/util-test.ts (describe "rank/util" > "longestPath").
public class RankUtilTests
{
    Graph g = null!;

    [Before(Test)]
    public void Setup() =>
        g = new Graph()
            .SetDefaultNodeLabel(_ => new())
            .SetDefaultEdgeLabel((_, _, _) => new() { Minlen = 1 });

    [Test]
    public async Task CanAssignARankToASingleNodeGraph()
    {
        g.SetNode("a");
        RankUtil.LongestPath(g);
        Util.NormalizeRanks(g);
        await Assert.That(g.Node("a").Rank).IsEqualTo(0);
    }

    [Test]
    public async Task CanAssignRanksToUnconnectedNodes()
    {
        g.SetNode("a");
        g.SetNode("b");
        RankUtil.LongestPath(g);
        Util.NormalizeRanks(g);
        await Assert.That(g.Node("a").Rank).IsEqualTo(0);
        await Assert.That(g.Node("b").Rank).IsEqualTo(0);
    }

    [Test]
    public async Task CanAssignRanksToConnectedNodes()
    {
        g.SetEdge("a", "b");
        RankUtil.LongestPath(g);
        Util.NormalizeRanks(g);
        await Assert.That(g.Node("a").Rank).IsEqualTo(0);
        await Assert.That(g.Node("b").Rank).IsEqualTo(1);
    }

    [Test]
    public async Task CanAssignRanksForADiamond()
    {
        g.SetPath(["a", "b", "d"]);
        g.SetPath(["a", "c", "d"]);
        RankUtil.LongestPath(g);
        Util.NormalizeRanks(g);
        await Assert.That(g.Node("a").Rank).IsEqualTo(0);
        await Assert.That(g.Node("b").Rank).IsEqualTo(1);
        await Assert.That(g.Node("c").Rank).IsEqualTo(1);
        await Assert.That(g.Node("d").Rank).IsEqualTo(2);
    }

    [Test]
    public async Task UsesTheMinlenAttributeOnTheEdge()
    {
        g.SetPath(["a", "b", "d"]);
        g.SetEdge("a", "c");
        g.SetEdge("c", "d", new() { Minlen = 2 });
        RankUtil.LongestPath(g);
        Util.NormalizeRanks(g);
        await Assert.That(g.Node("a").Rank).IsEqualTo(0);
        // longest path biases towards the lowest rank it can assign
        await Assert.That(g.Node("b").Rank).IsEqualTo(2);
        await Assert.That(g.Node("c").Rank).IsEqualTo(1);
        await Assert.That(g.Node("d").Rank).IsEqualTo(3);
    }
}
