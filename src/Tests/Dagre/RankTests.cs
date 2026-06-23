// Naiad's Dagre uses a single ranker (network-simplex), so these exercise the rank phase directly.
public class RankTests
{
    Graph graph = null!;

    [Before(Test)]
    public void Setup()
    {
        graph = new();
        graph.SetGraph(new());
        graph.SetDefaultNodeLabel(_ => new());
        graph.SetDefaultEdgeLabel((_, _, _) => new() {Minlen = 1, Weight = 1});
        graph
            .SetPath(["a", "b", "c", "d", "h"])
            .SetPath(["a", "e", "graph", "h"])
            .SetPath(["a", "f", "graph"]);
    }

    [Test]
    public async Task RespectsTheMinlenAttribute()
    {
        Rank.Run(graph);
        foreach (var e in graph.Edges())
        {
            var vRank = graph.NodeLabel(e.V).Rank!.Value;
            var wRank = graph.NodeLabel(e.W).Rank!.Value;
            await Assert.That(wRank - vRank).IsGreaterThanOrEqualTo(graph.FindEdgeLabel(e).Minlen!.Value);
        }
    }

    [Test]
    public async Task CanRankASingleNodeGraph()
    {
        var single = new Graph();
        single.SetGraph(new());
        single.SetNode("a", new());
        Rank.Run(single);
        await Assert.That(single.NodeLabel("a").Rank).IsEqualTo(0);
    }
}
