namespace Naiad.Dagre.Tests;

// Port of dagre test/rank/rank-test.ts (describe "rank").
// The TS test loops over RANKERS = ["longest-path", "tight-tree", "network-simplex",
// "unknown-should-still-work"], running two cases per ranker. Each (ranker, case) pair
// is expanded into its own [Test] here.
public class RankTests
{
    Graph g = null!;

    [Before(Test)]
    public void Setup()
    {
        g = new Graph()
            .SetGraph(new())
            .SetDefaultNodeLabel(_ => new())
            .SetDefaultEdgeLabel((_, _, _) => new() { Minlen = 1, Weight = 1 });
        g
            .SetPath(["a", "b", "c", "d", "h"])
            .SetPath(["a", "e", "g", "h"])
            .SetPath(["a", "f", "g"]);
    }

    // ---- longest-path ----
    [Test]
    public Task LongestPath_RespectsTheMinlenAttribute() =>
        RespectsTheMinlenAttribute("longest-path");

    [Test]
    public Task LongestPath_CanRankASingleNodeGraph() =>
        CanRankASingleNodeGraph("longest-path");

    // ---- tight-tree ----
    [Test]
    public Task TightTree_RespectsTheMinlenAttribute() =>
        RespectsTheMinlenAttribute("tight-tree");

    [Test]
    public Task TightTree_CanRankASingleNodeGraph() =>
        CanRankASingleNodeGraph("tight-tree");

    // ---- network-simplex ----
    [Test]
    public Task NetworkSimplex_RespectsTheMinlenAttribute() =>
        RespectsTheMinlenAttribute("network-simplex");

    [Test]
    public Task NetworkSimplex_CanRankASingleNodeGraph() =>
        CanRankASingleNodeGraph("network-simplex");

    // ---- unknown-should-still-work ----
    [Test]
    public Task Unknown_RespectsTheMinlenAttribute() =>
        RespectsTheMinlenAttribute("unknown-should-still-work");

    [Test]
    public Task Unknown_CanRankASingleNodeGraph() =>
        CanRankASingleNodeGraph("unknown-should-still-work");

    async Task RespectsTheMinlenAttribute(string ranker)
    {
        g.Graph_().Ranker = ranker;
        Rank.Run(g);
        foreach (var e in g.Edges())
        {
            var vRank = g.Node(e.V).Rank!.Value;
            var wRank = g.Node(e.W).Rank!.Value;
            await Assert.That(wRank - vRank).IsGreaterThanOrEqualTo(g.FindEdgeLabel(e).Minlen!.Value);
        }
    }

    static async Task CanRankASingleNodeGraph(string ranker)
    {
        // (no ranker, so the default ranker runs);
        // `ranker` is only written onto the node and ignored by the algorithm.
        // Rank is int? in C#, so the string assignment from the TS test is a no-op
        // (the algorithm overwrites the node's rank regardless).
        _ = ranker;
        var g = new Graph().SetGraph(new());
        g.SetNode("a", new());
        Rank.Run(g);
        await Assert.That(g.Node("a").Rank).IsEqualTo(0);
    }
}
