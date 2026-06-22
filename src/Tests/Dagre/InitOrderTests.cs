public class InitOrderTests
{
    Graph graph = null!;

    [Before(Test)]
    public void Setup() =>
        graph = new Graph(compound: true)
            .SetDefaultEdgeLabel((_, _, _) => new() { Weight = 1 });

    static List<string> Sorted(List<string> vs) =>
        vs.OrderBy(v => v, StringComparer.Ordinal).ToList();

    static string Join(IEnumerable<string> vs) => string.Join(",", vs);

    [Test]
    public async Task AssignsNonOverlappingOrdersForEachRankInATree()
    {
        foreach (var (v, rank) in new[] { ("a", 0), ("b", 1), ("c", 2), ("d", 2), ("e", 1) })
        {
            graph.SetNode(v, new() { Rank = rank });
        }

        graph.SetPath(["a", "b", "c"]);
        graph.SetEdge("b", "d");
        graph.SetEdge("a", "e");

        var layering = InitOrder.Run(graph);
        await Assert.That(Join(layering[0])).IsEqualTo("a");
        await Assert.That(Join(Sorted(layering[1]))).IsEqualTo("b,e");
        await Assert.That(Join(Sorted(layering[2]))).IsEqualTo("c,d");
    }

    [Test]
    public async Task AssignsNonOverlappingOrdersForEachRankInADAG()
    {
        foreach (var (v, rank) in new[] { ("a", 0), ("b", 1), ("c", 1), ("d", 2) })
        {
            graph.SetNode(v, new() { Rank = rank });
        }

        graph.SetPath(["a", "b", "d"]);
        graph.SetPath(["a", "c", "d"]);

        var layering = InitOrder.Run(graph);
        await Assert.That(Join(layering[0])).IsEqualTo("a");
        await Assert.That(Join(Sorted(layering[1]))).IsEqualTo("b,c");
        await Assert.That(Join(Sorted(layering[2]))).IsEqualTo("d");
    }

    [Test]
    public async Task DoesNotAssignAnOrderToSubgraphNodes()
    {
        graph.SetNode("a", new() { Rank = 0 });
        graph.SetNode("sg1", new());
        graph.SetParent("a", "sg1");

        var layering = InitOrder.Run(graph);
        await Assert.That(layering.Count).IsEqualTo(1);
        await Assert.That(Join(layering[0])).IsEqualTo("a");
    }
}
