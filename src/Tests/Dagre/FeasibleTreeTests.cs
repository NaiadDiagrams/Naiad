namespace Naiad.Dagre.Tests;

// Port of dagre test/rank/feasible-tree-test.ts (describe "feasibleTree").
public class FeasibleTreeTests
{
    [Test]
    public async Task CreatesATreeForATrivialInputGraph()
    {
        var g = new Graph()
            .SetNode("a", new() { Rank = 0 })
            .SetNode("b", new() { Rank = 1 });
        g.SetEdge("a", "b", new() { Minlen = 1 });

        var tree = FeasibleTree.Run(g);
        await Assert.That(g.NodeLabel("b").Rank).IsEqualTo(g.NodeLabel("a").Rank!.Value + 1);
        await Assert.That(tree.Neighbors("a")).IsEquivalentTo(new List<string> { "b" });
    }

    [Test]
    public async Task CorrectlyShortensSlackByPullingANodeUp()
    {
        var g = new Graph()
            .SetNode("a", new() { Rank = 0 })
            .SetNode("b", new() { Rank = 1 })
            .SetNode("c", new() { Rank = 2 })
            .SetNode("d", new() { Rank = 2 });
        g.SetPath(["a", "b", "c"], new() { Minlen = 1 });
        g.SetEdge("a", "d", new() { Minlen = 1 });

        var tree = FeasibleTree.Run(g);
        await Assert.That(g.NodeLabel("b").Rank).IsEqualTo(g.NodeLabel("a").Rank!.Value + 1);
        await Assert.That(g.NodeLabel("c").Rank).IsEqualTo(g.NodeLabel("b").Rank!.Value + 1);
        await Assert.That(g.NodeLabel("d").Rank).IsEqualTo(g.NodeLabel("a").Rank!.Value + 1);
        await Assert.That(Sorted(tree.Neighbors("a"))).IsEquivalentTo(new List<string> { "b", "d" });
        await Assert.That(Sorted(tree.Neighbors("b"))).IsEquivalentTo(new List<string> { "a", "c" });
        await Assert.That(tree.Neighbors("c")).IsEquivalentTo(new List<string> { "b" });
        await Assert.That(tree.Neighbors("d")).IsEquivalentTo(new List<string> { "a" });
    }

    [Test]
    public async Task CorrectlyShortensSlackByPullingANodeDown()
    {
        var g = new Graph()
            .SetNode("a", new() { Rank = 2 })
            .SetNode("b", new() { Rank = 0 })
            .SetNode("c", new() { Rank = 2 });
        g.SetEdge("b", "a", new() { Minlen = 1 });
        g.SetEdge("b", "c", new() { Minlen = 1 });

        var tree = FeasibleTree.Run(g);
        await Assert.That(g.NodeLabel("a").Rank).IsEqualTo(g.NodeLabel("b").Rank!.Value + 1);
        await Assert.That(g.NodeLabel("c").Rank).IsEqualTo(g.NodeLabel("b").Rank!.Value + 1);
        await Assert.That(Sorted(tree.Neighbors("a"))).IsEquivalentTo(new List<string> { "b" });
        await Assert.That(Sorted(tree.Neighbors("b"))).IsEquivalentTo(new List<string> { "a", "c" });
        await Assert.That(Sorted(tree.Neighbors("c"))).IsEquivalentTo(new List<string> { "b" });
    }

    static List<string> Sorted(List<string>? values)
    {
        var copy = new List<string>(values!);
        copy.Sort(StringComparer.Ordinal);
        return copy;
    }
}
