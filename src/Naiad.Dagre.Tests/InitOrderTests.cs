namespace Naiad.Dagre.Tests;

public class InitOrderTests
{
    Graph g = null!;

    [Before(Test)]
    public void Setup() =>
        g = new Graph(compound: true)
            .SetDefaultEdgeLabel((_, _, _) => new EdgeLabel { Weight = 1 });

    static List<string> Sorted(List<string> vs) =>
        vs.OrderBy(v => v, StringComparer.Ordinal).ToList();

    static string Join(IEnumerable<string> vs) => string.Join(",", vs);

    [Test]
    public async Task AssignsNonOverlappingOrdersForEachRankInATree()
    {
        foreach (var (v, rank) in new[] { ("a", 0), ("b", 1), ("c", 2), ("d", 2), ("e", 1) })
        {
            g.SetNode(v, new NodeLabel { Rank = rank });
        }

        g.SetPath(["a", "b", "c"]);
        g.SetEdge("b", "d");
        g.SetEdge("a", "e");

        var layering = InitOrder.Run(g);
        await Assert.That(Join(layering[0])).IsEqualTo("a");
        await Assert.That(Join(Sorted(layering[1]))).IsEqualTo("b,e");
        await Assert.That(Join(Sorted(layering[2]))).IsEqualTo("c,d");
    }

    [Test]
    public async Task AssignsNonOverlappingOrdersForEachRankInADAG()
    {
        foreach (var (v, rank) in new[] { ("a", 0), ("b", 1), ("c", 1), ("d", 2) })
        {
            g.SetNode(v, new NodeLabel { Rank = rank });
        }

        g.SetPath(["a", "b", "d"]);
        g.SetPath(["a", "c", "d"]);

        var layering = InitOrder.Run(g);
        await Assert.That(Join(layering[0])).IsEqualTo("a");
        await Assert.That(Join(Sorted(layering[1]))).IsEqualTo("b,c");
        await Assert.That(Join(Sorted(layering[2]))).IsEqualTo("d");
    }

    [Test]
    public async Task DoesNotAssignAnOrderToSubgraphNodes()
    {
        g.SetNode("a", new NodeLabel { Rank = 0 });
        g.SetNode("sg1", new NodeLabel());
        g.SetParent("a", "sg1");

        var layering = InitOrder.Run(g);
        await Assert.That(layering.Count).IsEqualTo(1);
        await Assert.That(Join(layering[0])).IsEqualTo("a");
    }
}
