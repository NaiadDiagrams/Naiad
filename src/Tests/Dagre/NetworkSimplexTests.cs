namespace Naiad.Dagre.Tests;

// Port of dagre test/rank/network-simplex-test.ts (describe "network simplex").
public class NetworkSimplexTests
{
    Graph g = null!;
    Graph t = null!;
    Graph gansnerGraph = null!;
    Graph gansnerTree = null!;

    [Before(Test)]
    public void Setup()
    {
        g = new Graph(multigraph: true)
            .SetDefaultNodeLabel(_ => new())
            .SetDefaultEdgeLabel((_, _, _) => new() { Minlen = 1, Weight = 1 });

        t = new Graph(directed: false)
            .SetDefaultNodeLabel(_ => new())
            .SetDefaultEdgeLabel((_, _, _) => new());

        gansnerGraph = new Graph()
            .SetDefaultNodeLabel(_ => new())
            .SetDefaultEdgeLabel((_, _, _) => new() { Minlen = 1, Weight = 1 });
        gansnerGraph
            .SetPath(["a", "b", "c", "d", "h"])
            .SetPath(["a", "e", "g", "h"])
            .SetPath(["a", "f", "g"]);

        gansnerTree = new Graph(directed: false)
            .SetDefaultNodeLabel(_ => new())
            .SetDefaultEdgeLabel((_, _, _) => new());
        gansnerTree
            .SetPath(["a", "b", "c", "d", "h", "g", "e"])
            .SetEdge("g", "f");
    }

    [Test]
    public async Task CanAssignARankToASingleNode()
    {
        g.SetNode("a");
        Ns(g);
        await Assert.That(g.Node("a").Rank).IsEqualTo(0);
    }

    [Test]
    public async Task CanAssignARankToA2NodeConnectedGraph()
    {
        g.SetEdge("a", "b");
        Ns(g);
        await Assert.That(g.Node("a").Rank).IsEqualTo(0);
        await Assert.That(g.Node("b").Rank).IsEqualTo(1);
    }

    [Test]
    public async Task CanAssignRanksForADiamond()
    {
        g.SetPath(["a", "b", "d"]);
        g.SetPath(["a", "c", "d"]);
        Ns(g);
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
        Ns(g);
        await Assert.That(g.Node("a").Rank).IsEqualTo(0);
        // longest path biases towards the lowest rank it can assign. Since the
        // graph has no optimization opportunities we can assume that the longest
        // path ranking is used.
        await Assert.That(g.Node("b").Rank).IsEqualTo(2);
        await Assert.That(g.Node("c").Rank).IsEqualTo(1);
        await Assert.That(g.Node("d").Rank).IsEqualTo(3);
    }

    [Test]
    public async Task CanRankTheGansnerGraph()
    {
        g = gansnerGraph;
        Ns(g);
        await Assert.That(g.Node("a").Rank).IsEqualTo(0);
        await Assert.That(g.Node("b").Rank).IsEqualTo(1);
        await Assert.That(g.Node("c").Rank).IsEqualTo(2);
        await Assert.That(g.Node("d").Rank).IsEqualTo(3);
        await Assert.That(g.Node("h").Rank).IsEqualTo(4);
        await Assert.That(g.Node("e").Rank).IsEqualTo(1);
        await Assert.That(g.Node("f").Rank).IsEqualTo(1);
        await Assert.That(g.Node("g").Rank).IsEqualTo(2);
    }

    [Test]
    public async Task CanHandleMultiEdges()
    {
        g.SetPath(["a", "b", "c", "d"]);
        g.SetEdge("a", "e", new() { Weight = 2, Minlen = 1 });
        g.SetEdge("e", "d");
        g.SetEdge("b", "c", new() { Weight = 1, Minlen = 2 }, "multi");
        Ns(g);
        await Assert.That(g.Node("a").Rank).IsEqualTo(0);
        await Assert.That(g.Node("b").Rank).IsEqualTo(1);
        // b -> c has minlen = 1 and minlen = 2, so it should be 2 ranks apart.
        await Assert.That(g.Node("c").Rank).IsEqualTo(3);
        await Assert.That(g.Node("d").Rank).IsEqualTo(4);
        await Assert.That(g.Node("e").Rank).IsEqualTo(1);
    }

    // describe("leaveEdge")
    [Test]
    public async Task LeaveEdge_ReturnsUndefinedIfThereIsNoEdgeWithANegativeCutvalue()
    {
        var tree = new Graph(directed: false);
        tree.SetEdge("a", "b", new() { Cutvalue = 1 });
        tree.SetEdge("b", "c", new() { Cutvalue = 1 });
        await Assert.That(NetworkSimplex.LeaveEdge(tree)).IsNull();
    }

    [Test]
    public async Task LeaveEdge_ReturnsAnEdgeIfOneIsFoundWithANegativeCutvalue()
    {
        var tree = new Graph(directed: false);
        tree.SetEdge("a", "b", new() { Cutvalue = 1 });
        tree.SetEdge("b", "c", new() { Cutvalue = -1 });
        var e = NetworkSimplex.LeaveEdge(tree);
        await Assert.That(e).IsNotNull();
        await Assert.That(e!.V).IsEqualTo("b");
        await Assert.That(e.W).IsEqualTo("c");
    }

    // describe("enterEdge")
    [Test]
    public async Task EnterEdge_FindsAnEdgeFromTheHeadToTailComponent()
    {
        g
            .SetNode("a", new() { Rank = 0 })
            .SetNode("b", new() { Rank = 2 })
            .SetNode("c", new() { Rank = 3 })
            .SetPath(["a", "b", "c"])
            .SetEdge("a", "c");
        t.SetPath(["b", "c", "a"]);
        NetworkSimplex.InitLowLimValues(t, "c");

        var f = NetworkSimplex.EnterEdge(t, g, new("b", "c"));
        await Assert.That(UndirectedEdge(f)).IsEqualTo(UndirectedEdge(new("a", "b")));
    }

    [Test]
    public async Task EnterEdge_WorksWhenTheRootOfTheTreeIsInTheTailComponent()
    {
        g
            .SetNode("a", new() { Rank = 0 })
            .SetNode("b", new() { Rank = 2 })
            .SetNode("c", new() { Rank = 3 })
            .SetPath(["a", "b", "c"])
            .SetEdge("a", "c");
        t.SetPath(["b", "c", "a"]);
        NetworkSimplex.InitLowLimValues(t, "b");

        var f = NetworkSimplex.EnterEdge(t, g, new("b", "c"));
        await Assert.That(UndirectedEdge(f)).IsEqualTo(UndirectedEdge(new("a", "b")));
    }

    [Test]
    public async Task EnterEdge_FindsTheEdgeWithTheLeastSlack()
    {
        g
            .SetNode("a", new() { Rank = 0 })
            .SetNode("b", new() { Rank = 1 })
            .SetNode("c", new() { Rank = 3 })
            .SetNode("d", new() { Rank = 4 })
            .SetEdge("a", "d")
            .SetPath(["a", "c", "d"])
            .SetEdge("b", "c");
        t.SetPath(["c", "d", "a", "b"]);
        NetworkSimplex.InitLowLimValues(t, "a");

        var f = NetworkSimplex.EnterEdge(t, g, new("c", "d"));
        await Assert.That(UndirectedEdge(f)).IsEqualTo(UndirectedEdge(new("b", "c")));
    }

    [Test]
    public async Task EnterEdge_FindsAnAppropriateEdgeForGansnerGraph1()
    {
        g = gansnerGraph;
        t = gansnerTree;
        RankUtil.LongestPath(g);
        NetworkSimplex.InitLowLimValues(t, "a");

        var f = NetworkSimplex.EnterEdge(t, g, new("g", "h"));
        await Assert.That(UndirectedEdge(f).V).IsEqualTo("a");
        await Assert.That(["e", "f"]).Contains(UndirectedEdge(f).W);
    }

    [Test]
    public async Task EnterEdge_FindsAnAppropriateEdgeForGansnerGraph2()
    {
        g = gansnerGraph;
        t = gansnerTree;
        RankUtil.LongestPath(g);
        NetworkSimplex.InitLowLimValues(t, "e");

        var f = NetworkSimplex.EnterEdge(t, g, new("g", "h"));
        await Assert.That(UndirectedEdge(f).V).IsEqualTo("a");
        await Assert.That(["e", "f"]).Contains(UndirectedEdge(f).W);
    }

    [Test]
    public async Task EnterEdge_FindsAnAppropriateEdgeForGansnerGraph3()
    {
        g = gansnerGraph;
        t = gansnerTree;
        RankUtil.LongestPath(g);
        NetworkSimplex.InitLowLimValues(t, "a");

        var f = NetworkSimplex.EnterEdge(t, g, new("h", "g"));
        await Assert.That(UndirectedEdge(f).V).IsEqualTo("a");
        await Assert.That(["e", "f"]).Contains(UndirectedEdge(f).W);
    }

    [Test]
    public async Task EnterEdge_FindsAnAppropriateEdgeForGansnerGraph4()
    {
        g = gansnerGraph;
        t = gansnerTree;
        RankUtil.LongestPath(g);
        NetworkSimplex.InitLowLimValues(t, "e");

        var f = NetworkSimplex.EnterEdge(t, g, new("h", "g"));
        await Assert.That(UndirectedEdge(f).V).IsEqualTo("a");
        await Assert.That(["e", "f"]).Contains(UndirectedEdge(f).W);
    }

    // describe("initLowLimValues")
    [Test]
    public async Task InitLowLimValues_AssignsLowLimAndParentForEachNodeInATree()
    {
        var graph = new Graph()
            .SetDefaultNodeLabel(_ => new());
        graph
            .SetNodes(["a", "b", "c", "d", "e"])
            .SetPath(["a", "b", "a", "c", "d", "c", "e"]);

        NetworkSimplex.InitLowLimValues(graph, "a");

        var a = graph.Node("a");
        var b = graph.Node("b");
        var c = graph.Node("c");
        var d = graph.Node("d");
        var e = graph.Node("e");

        var lims = graph.Nodes().Select(v => graph.Node(v).Lim!.Value).ToList();
        lims.Sort();
        await Assert.That(lims).IsEquivalentTo(new List<int> { 1, 2, 3, 4, 5 });

        // a should be {low: 1, lim: 5} (root has no parent)
        await Assert.That(a.Low).IsEqualTo(1);
        await Assert.That(a.Lim).IsEqualTo(5);
        await Assert.That(a.Parent).IsNull();

        await Assert.That(b.Parent).IsEqualTo("a");
        await Assert.That(b.Lim!.Value).IsLessThan(a.Lim!.Value);

        await Assert.That(c.Parent).IsEqualTo("a");
        await Assert.That(c.Lim!.Value).IsLessThan(a.Lim!.Value);
        await Assert.That(c.Lim!.Value).IsNotEqualTo(b.Lim!.Value);

        await Assert.That(d.Parent).IsEqualTo("c");
        await Assert.That(d.Lim!.Value).IsLessThan(c.Lim!.Value);

        await Assert.That(e.Parent).IsEqualTo("c");
        await Assert.That(e.Lim!.Value).IsLessThan(c.Lim!.Value);
        await Assert.That(e.Lim!.Value).IsNotEqualTo(d.Lim!.Value);
    }

    // describe("exchangeEdges")
    [Test]
    public async Task ExchangeEdges_ExchangesEdgesAndUpdatesCutValuesAndLowLimNumbers()
    {
        g = gansnerGraph;
        t = gansnerTree;
        RankUtil.LongestPath(g);
        NetworkSimplex.InitLowLimValues(t);

        NetworkSimplex.ExchangeEdges(t, g, new("g", "h"), new("a", "e"));

        // check new cut values
        await Assert.That(t.Edge_("a", "b").Cutvalue).IsEqualTo(2);
        await Assert.That(t.Edge_("b", "c").Cutvalue).IsEqualTo(2);
        await Assert.That(t.Edge_("c", "d").Cutvalue).IsEqualTo(2);
        await Assert.That(t.Edge_("d", "h").Cutvalue).IsEqualTo(2);
        await Assert.That(t.Edge_("a", "e").Cutvalue).IsEqualTo(1);
        await Assert.That(t.Edge_("e", "g").Cutvalue).IsEqualTo(1);
        await Assert.That(t.Edge_("g", "f").Cutvalue).IsEqualTo(0);

        // ensure lim numbers look right
        var lims = t.Nodes().Select(v => t.Node(v).Lim!.Value).ToList();
        lims.Sort();
        await Assert.That(lims).IsEquivalentTo(new List<int> { 1, 2, 3, 4, 5, 6, 7, 8 });
    }

    [Test]
    public async Task ExchangeEdges_UpdatesRanks()
    {
        g = gansnerGraph;
        t = gansnerTree;
        RankUtil.LongestPath(g);
        NetworkSimplex.InitLowLimValues(t);

        NetworkSimplex.ExchangeEdges(t, g, new("g", "h"), new("a", "e"));
        Util.NormalizeRanks(g);

        // check new ranks
        await Assert.That(g.Node("a").Rank).IsEqualTo(0);
        await Assert.That(g.Node("b").Rank).IsEqualTo(1);
        await Assert.That(g.Node("c").Rank).IsEqualTo(2);
        await Assert.That(g.Node("d").Rank).IsEqualTo(3);
        await Assert.That(g.Node("e").Rank).IsEqualTo(1);
        await Assert.That(g.Node("f").Rank).IsEqualTo(1);
        await Assert.That(g.Node("g").Rank).IsEqualTo(2);
        await Assert.That(g.Node("h").Rank).IsEqualTo(4);
    }

    // describe("calcCutValue")
    // Note: we use p for parent, c for child, gc_x for grandchild nodes, and o for
    // other nodes in the tree for these tests.
    [Test]
    public async Task CalcCutValue_WorksForA2NodeTreeWithCToP()
    {
        g.SetPath(["c", "p"]);
        t.SetPath(["p", "c"]);
        NetworkSimplex.InitLowLimValues(t, "p");

        await Assert.That(NetworkSimplex.CalcCutValue(t, g, "c")).IsEqualTo(1);
    }

    [Test]
    public async Task CalcCutValue_WorksForA2NodeTreeWithCFromP()
    {
        g.SetPath(["p", "c"]);
        t.SetPath(["p", "c"]);
        NetworkSimplex.InitLowLimValues(t, "p");

        await Assert.That(NetworkSimplex.CalcCutValue(t, g, "c")).IsEqualTo(1);
    }

    [Test]
    public async Task CalcCutValue_WorksFor3NodeTreeWithGcToCToP()
    {
        g.SetPath(["gc", "c", "p"]);
        t
            .SetEdge("gc", "c", new() { Cutvalue = 3 })
            .SetEdge("p", "c");
        NetworkSimplex.InitLowLimValues(t, "p");

        await Assert.That(NetworkSimplex.CalcCutValue(t, g, "c")).IsEqualTo(3);
    }

    [Test]
    public async Task CalcCutValue_WorksFor3NodeTreeWithGcToCFromP()
    {
        g
            .SetEdge("p", "c")
            .SetEdge("gc", "c");
        t
            .SetEdge("gc", "c", new() { Cutvalue = 3 })
            .SetEdge("p", "c");
        NetworkSimplex.InitLowLimValues(t, "p");

        await Assert.That(NetworkSimplex.CalcCutValue(t, g, "c")).IsEqualTo(-1);
    }

    [Test]
    public async Task CalcCutValue_WorksFor3NodeTreeWithGcFromCToP()
    {
        g
            .SetEdge("c", "p")
            .SetEdge("c", "gc");
        t
            .SetEdge("gc", "c", new() { Cutvalue = 3 })
            .SetEdge("p", "c");
        NetworkSimplex.InitLowLimValues(t, "p");

        await Assert.That(NetworkSimplex.CalcCutValue(t, g, "c")).IsEqualTo(-1);
    }

    [Test]
    public async Task CalcCutValue_WorksFor3NodeTreeWithGcFromCFromP()
    {
        g.SetPath(["p", "c", "gc"]);
        t
            .SetEdge("gc", "c", new() { Cutvalue = 3 })
            .SetEdge("p", "c");
        NetworkSimplex.InitLowLimValues(t, "p");

        await Assert.That(NetworkSimplex.CalcCutValue(t, g, "c")).IsEqualTo(3);
    }

    [Test]
    public async Task CalcCutValue_WorksFor4NodeTreeGcToCToPToO_WithOToC()
    {
        g
            .SetEdge("o", "c", new() { Weight = 7 })
            .SetPath(["gc", "c", "p", "o"]);
        t
            .SetEdge("gc", "c", new() { Cutvalue = 3 })
            .SetPath(["c", "p", "o"]);
        NetworkSimplex.InitLowLimValues(t, "p");

        await Assert.That(NetworkSimplex.CalcCutValue(t, g, "c")).IsEqualTo(-4);
    }

    [Test]
    public async Task CalcCutValue_WorksFor4NodeTreeGcToCToPToO_WithOFromC()
    {
        g
            .SetEdge("c", "o", new() { Weight = 7 })
            .SetPath(["gc", "c", "p", "o"]);
        t
            .SetEdge("gc", "c", new() { Cutvalue = 3 })
            .SetPath(["c", "p", "o"]);
        NetworkSimplex.InitLowLimValues(t, "p");

        await Assert.That(NetworkSimplex.CalcCutValue(t, g, "c")).IsEqualTo(10);
    }

    [Test]
    public async Task CalcCutValue_WorksFor4NodeTreeOToGcToCToP_WithOToC()
    {
        g
            .SetEdge("o", "c", new() { Weight = 7 })
            .SetPath(["o", "gc", "c", "p"]);
        t
            .SetEdge("o", "gc")
            .SetEdge("gc", "c", new() { Cutvalue = 3 })
            .SetEdge("c", "p");
        NetworkSimplex.InitLowLimValues(t, "p");

        await Assert.That(NetworkSimplex.CalcCutValue(t, g, "c")).IsEqualTo(-4);
    }

    [Test]
    public async Task CalcCutValue_WorksFor4NodeTreeOToGcToCToP_WithOFromC()
    {
        g
            .SetEdge("c", "o", new() { Weight = 7 })
            .SetPath(["o", "gc", "c", "p"]);
        t
            .SetEdge("o", "gc")
            .SetEdge("gc", "c", new() { Cutvalue = 3 })
            .SetEdge("c", "p");
        NetworkSimplex.InitLowLimValues(t, "p");

        await Assert.That(NetworkSimplex.CalcCutValue(t, g, "c")).IsEqualTo(10);
    }

    [Test]
    public async Task CalcCutValue_WorksFor4NodeTreeGcToCFromPToO_WithOToC()
    {
        g
            .SetEdge("gc", "c")
            .SetEdge("p", "c")
            .SetEdge("p", "o")
            .SetEdge("o", "c", new() { Weight = 7 });
        t
            .SetEdge("o", "gc")
            .SetEdge("gc", "c", new() { Cutvalue = 3 })
            .SetEdge("c", "p");
        NetworkSimplex.InitLowLimValues(t, "p");

        await Assert.That(NetworkSimplex.CalcCutValue(t, g, "c")).IsEqualTo(6);
    }

    [Test]
    public async Task CalcCutValue_WorksFor4NodeTreeGcToCFromPToO_WithOFromC()
    {
        g
            .SetEdge("gc", "c")
            .SetEdge("p", "c")
            .SetEdge("p", "o")
            .SetEdge("c", "o", new() { Weight = 7 });
        t
            .SetEdge("o", "gc")
            .SetEdge("gc", "c", new() { Cutvalue = 3 })
            .SetEdge("c", "p");
        NetworkSimplex.InitLowLimValues(t, "p");

        await Assert.That(NetworkSimplex.CalcCutValue(t, g, "c")).IsEqualTo(-8);
    }

    [Test]
    public async Task CalcCutValue_WorksFor4NodeTreeOToGcToCFromP_WithOToC()
    {
        g
            .SetEdge("o", "c", new() { Weight = 7 })
            .SetPath(["o", "gc", "c"])
            .SetEdge("p", "c");
        t
            .SetEdge("o", "gc")
            .SetEdge("gc", "c", new() { Cutvalue = 3 })
            .SetEdge("c", "p");
        NetworkSimplex.InitLowLimValues(t, "p");

        await Assert.That(NetworkSimplex.CalcCutValue(t, g, "c")).IsEqualTo(6);
    }

    [Test]
    public async Task CalcCutValue_WorksFor4NodeTreeOToGcToCFromP_WithOFromC()
    {
        g
            .SetEdge("c", "o", new() { Weight = 7 })
            .SetPath(["o", "gc", "c"])
            .SetEdge("p", "c");
        t
            .SetEdge("o", "gc")
            .SetEdge("gc", "c", new() { Cutvalue = 3 })
            .SetEdge("c", "p");
        NetworkSimplex.InitLowLimValues(t, "p");

        await Assert.That(NetworkSimplex.CalcCutValue(t, g, "c")).IsEqualTo(-8);
    }

    // describe("initCutValues")
    [Test]
    public async Task InitCutValues_WorksForGansnerGraph()
    {
        NetworkSimplex.InitLowLimValues(gansnerTree);
        NetworkSimplex.InitCutValues(gansnerTree, gansnerGraph);
        await Assert.That(gansnerTree.Edge_("a", "b").Cutvalue).IsEqualTo(3);
        await Assert.That(gansnerTree.Edge_("b", "c").Cutvalue).IsEqualTo(3);
        await Assert.That(gansnerTree.Edge_("c", "d").Cutvalue).IsEqualTo(3);
        await Assert.That(gansnerTree.Edge_("d", "h").Cutvalue).IsEqualTo(3);
        await Assert.That(gansnerTree.Edge_("g", "h").Cutvalue).IsEqualTo(-1);
        await Assert.That(gansnerTree.Edge_("e", "g").Cutvalue).IsEqualTo(0);
        await Assert.That(gansnerTree.Edge_("f", "g").Cutvalue).IsEqualTo(0);
    }

    [Test]
    public async Task InitCutValues_WorksForUpdatedGansnerGraph()
    {
        gansnerTree.RemoveEdge("g", "h");
        gansnerTree.SetEdge("a", "e");
        NetworkSimplex.InitLowLimValues(gansnerTree);
        NetworkSimplex.InitCutValues(gansnerTree, gansnerGraph);
        await Assert.That(gansnerTree.Edge_("a", "b").Cutvalue).IsEqualTo(2);
        await Assert.That(gansnerTree.Edge_("b", "c").Cutvalue).IsEqualTo(2);
        await Assert.That(gansnerTree.Edge_("c", "d").Cutvalue).IsEqualTo(2);
        await Assert.That(gansnerTree.Edge_("d", "h").Cutvalue).IsEqualTo(2);
        await Assert.That(gansnerTree.Edge_("a", "e").Cutvalue).IsEqualTo(1);
        await Assert.That(gansnerTree.Edge_("e", "g").Cutvalue).IsEqualTo(1);
        await Assert.That(gansnerTree.Edge_("f", "g").Cutvalue).IsEqualTo(0);
    }

    static void Ns(Graph graph)
    {
        NetworkSimplex.Run(graph);
        Util.NormalizeRanks(graph);
    }

    static Edge UndirectedEdge(Edge e) =>
        string.CompareOrdinal(e.V, e.W) < 0 ? new(e.V, e.W) : new Edge(e.W, e.V);
}
