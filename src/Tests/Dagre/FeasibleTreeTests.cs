public class FeasibleTreeTests
{
    [Test]
    public async Task CreatesATreeForATrivialInputGraph()
    {
        var graph = new Graph()
            .SetNode("a", new() { Rank = 0 })
            .SetNode("b", new() { Rank = 1 });
        graph.SetEdge("a", "b", new() { Minlen = 1 });

        var tree = FeasibleTree.Run(graph);
        await Assert.That(graph.NodeLabel("b").Rank).IsEqualTo(graph.NodeLabel("a").Rank!.Value + 1);
        await Assert.That(tree.Neighbors("a")).IsEquivalentTo(new List<string> { "b" });
    }

    [Test]
    public async Task CorrectlyShortensSlackByPullingANodeUp()
    {
        var graph = new Graph()
            .SetNode("a", new() { Rank = 0 })
            .SetNode("b", new() { Rank = 1 })
            .SetNode("c", new() { Rank = 2 })
            .SetNode("d", new() { Rank = 2 });
        graph.SetPath(["a", "b", "c"], new() { Minlen = 1 });
        graph.SetEdge("a", "d", new() { Minlen = 1 });

        var tree = FeasibleTree.Run(graph);
        await Assert.That(graph.NodeLabel("b").Rank).IsEqualTo(graph.NodeLabel("a").Rank!.Value + 1);
        await Assert.That(graph.NodeLabel("c").Rank).IsEqualTo(graph.NodeLabel("b").Rank!.Value + 1);
        await Assert.That(graph.NodeLabel("d").Rank).IsEqualTo(graph.NodeLabel("a").Rank!.Value + 1);
        await Assert.That(Sorted(tree.Neighbors("a"))).IsEquivalentTo(new List<string> { "b", "d" });
        await Assert.That(Sorted(tree.Neighbors("b"))).IsEquivalentTo(new List<string> { "a", "c" });
        await Assert.That(tree.Neighbors("c")).IsEquivalentTo(new List<string> { "b" });
        await Assert.That(tree.Neighbors("d")).IsEquivalentTo(new List<string> { "a" });
    }

    [Test]
    public async Task CorrectlyShortensSlackByPullingANodeDown()
    {
        var graph = new Graph()
            .SetNode("a", new() { Rank = 2 })
            .SetNode("b", new() { Rank = 0 })
            .SetNode("c", new() { Rank = 2 });
        graph.SetEdge("b", "a", new() { Minlen = 1 });
        graph.SetEdge("b", "c", new() { Minlen = 1 });

        var tree = FeasibleTree.Run(graph);
        await Assert.That(graph.NodeLabel("a").Rank).IsEqualTo(graph.NodeLabel("b").Rank!.Value + 1);
        await Assert.That(graph.NodeLabel("c").Rank).IsEqualTo(graph.NodeLabel("b").Rank!.Value + 1);
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
