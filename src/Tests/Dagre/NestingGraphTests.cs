namespace Naiad.Dagre.Tests;

public class NestingGraphTests
{
    Graph g = null!;

    [Before(Test)]
    public void Setup() =>
        g = new Graph(compound: true)
            .SetGraph(new())
            .SetDefaultNodeLabel(_ => new());

    // ----- run -----

    [Test]
    public async Task ConnectsADisconnectedGraph()
    {
        g.SetNode("a");
        g.SetNode("b");
        await Assert.That(Alg.Components(g).Count).IsEqualTo(2);
        NestingGraph.Run(g);
        await Assert.That(Alg.Components(g).Count).IsEqualTo(1);
        await Assert.That(g.HasNode("a")).IsTrue();
        await Assert.That(g.HasNode("b")).IsTrue();
    }

    [Test]
    public async Task AddsBorderNodesToTheTopAndBottomOfASubgraph()
    {
        g.SetParent("a", "sg1");
        NestingGraph.Run(g);

        var borderTop = g.Node("sg1").BorderTop;
        var borderBottom = g.Node("sg1").BorderBottom;
        await Assert.That(borderTop).IsNotNull();
        await Assert.That(borderBottom).IsNotNull();
        await Assert.That(g.Parent(borderTop!)).IsEqualTo("sg1");
        await Assert.That(g.Parent(borderBottom!)).IsEqualTo("sg1");

        var topToA = g.OutEdges(borderTop!, "a")!;
        await Assert.That(topToA.Count).IsEqualTo(1);
        await Assert.That(g.FindEdgeLabel(topToA[0]).Minlen).IsEqualTo(1);

        var aToBottom = g.OutEdges("a", borderBottom!)!;
        await Assert.That(aToBottom.Count).IsEqualTo(1);
        await Assert.That(g.FindEdgeLabel(aToBottom[0]).Minlen).IsEqualTo(1);

        var topNode = g.Node(borderTop!);
        await Assert.That(topNode.Width).IsEqualTo(0);
        await Assert.That(topNode.Height).IsEqualTo(0);
        await Assert.That(topNode.Dummy).IsEqualTo("border");

        var bottomNode = g.Node(borderBottom!);
        await Assert.That(bottomNode.Width).IsEqualTo(0);
        await Assert.That(bottomNode.Height).IsEqualTo(0);
        await Assert.That(bottomNode.Dummy).IsEqualTo("border");
    }

    [Test]
    public async Task AddsEdgesBetweenBordersOfNestedSubgraphs()
    {
        g.SetParent("sg2", "sg1");
        g.SetParent("a", "sg2");
        NestingGraph.Run(g);

        var sg1Top = g.Node("sg1").BorderTop;
        var sg1Bottom = g.Node("sg1").BorderBottom;
        var sg2Top = g.Node("sg2").BorderTop;
        var sg2Bottom = g.Node("sg2").BorderBottom;
        await Assert.That(sg1Top).IsNotNull();
        await Assert.That(sg1Bottom).IsNotNull();
        await Assert.That(sg2Top).IsNotNull();
        await Assert.That(sg2Bottom).IsNotNull();

        var sg1TopToSg2Top = g.OutEdges(sg1Top!, sg2Top!)!;
        await Assert.That(sg1TopToSg2Top.Count).IsEqualTo(1);
        await Assert.That(g.FindEdgeLabel(sg1TopToSg2Top[0]).Minlen).IsEqualTo(1);

        var sg2BottomToSg1Bottom = g.OutEdges(sg2Bottom!, sg1Bottom!)!;
        await Assert.That(sg2BottomToSg1Bottom.Count).IsEqualTo(1);
        await Assert.That(g.FindEdgeLabel(sg2BottomToSg1Bottom[0]).Minlen).IsEqualTo(1);
    }

    [Test]
    public async Task AddsSufficientWeightToBorderToNodeEdges()
    {
        // We want to keep subgraphs tight, so we should ensure that the weight for
        // the edge between the top (and bottom) border nodes and nodes in the
        // subgraph have weights exceeding anything in the graph.
        g.SetParent("x", "sg");
        g.SetEdge("a", "x", new() { Weight = 100 });
        g.SetEdge("x", "b", new() { Weight = 200 });
        NestingGraph.Run(g);

        var top = g.Node("sg").BorderTop;
        var bot = g.Node("sg").BorderBottom;
        await Assert.That(g.FindEdgeLabel(top!, "x").Weight!.Value).IsGreaterThan(300);
        await Assert.That(g.FindEdgeLabel("x", bot!).Weight!.Value).IsGreaterThan(300);
    }

    [Test]
    public async Task AddsAnEdgeFromTheRootToTheTopsOfTopLevelSubgraphs()
    {
        g.SetParent("a", "sg1");
        NestingGraph.Run(g);

        var root = g.GraphLabel.NestingRoot;
        var borderTop = g.Node("sg1").BorderTop;
        await Assert.That(root).IsNotNull();
        await Assert.That(borderTop).IsNotNull();

        var rootToTop = g.OutEdges(root!, borderTop!)!;
        await Assert.That(rootToTop.Count).IsEqualTo(1);
        await Assert.That(g.HasEdge(rootToTop[0])).IsTrue();
    }

    [Test]
    public async Task AddsAnEdgeFromRootToEachNodeWithTheCorrectMinlen1()
    {
        g.SetNode("a");
        NestingGraph.Run(g);

        var root = g.GraphLabel.NestingRoot;
        await Assert.That(root).IsNotNull();

        var rootToA = g.OutEdges(root!, "a")!;
        await Assert.That(rootToA.Count).IsEqualTo(1);

        var label = g.FindEdgeLabel(rootToA[0]);
        await Assert.That(label.Weight).IsEqualTo(0);
        await Assert.That(label.Minlen).IsEqualTo(1);
    }

    [Test]
    public async Task AddsAnEdgeFromRootToEachNodeWithTheCorrectMinlen2()
    {
        g.SetParent("a", "sg1");
        NestingGraph.Run(g);

        var root = g.GraphLabel.NestingRoot;
        await Assert.That(root).IsNotNull();

        var rootToA = g.OutEdges(root!, "a")!;
        await Assert.That(rootToA.Count).IsEqualTo(1);

        var label = g.FindEdgeLabel(rootToA[0]);
        await Assert.That(label.Weight).IsEqualTo(0);
        await Assert.That(label.Minlen).IsEqualTo(3);
    }

    [Test]
    public async Task AddsAnEdgeFromRootToEachNodeWithTheCorrectMinlen3()
    {
        g.SetParent("sg2", "sg1");
        g.SetParent("a", "sg2");
        NestingGraph.Run(g);

        var root = g.GraphLabel.NestingRoot;
        await Assert.That(root).IsNotNull();

        var rootToA = g.OutEdges(root!, "a")!;
        await Assert.That(rootToA.Count).IsEqualTo(1);

        var label = g.FindEdgeLabel(rootToA[0]);
        await Assert.That(label.Weight).IsEqualTo(0);
        await Assert.That(label.Minlen).IsEqualTo(5);
    }

    [Test]
    public async Task DoesNotAddAnEdgeFromTheRootToItself()
    {
        g.SetNode("a");
        NestingGraph.Run(g);

        var root = g.GraphLabel.NestingRoot;
        var rootToRoot = g.OutEdges(root!, root!)!;
        await Assert.That(rootToRoot.Count).IsEqualTo(0);
    }

    [Test]
    public async Task ExpandsInterNodeEdgesToSeparateSgBorderAndNodes1()
    {
        g.SetEdge("a", "b", new() { Minlen = 1 });
        NestingGraph.Run(g);
        await Assert.That(g.FindEdgeLabel("a", "b").Minlen).IsEqualTo(1);
    }

    [Test]
    public async Task ExpandsInterNodeEdgesToSeparateSgBorderAndNodes2()
    {
        g.SetParent("a", "sg1");
        g.SetEdge("a", "b", new() { Minlen = 1 });
        NestingGraph.Run(g);
        await Assert.That(g.FindEdgeLabel("a", "b").Minlen).IsEqualTo(3);
    }

    [Test]
    public async Task ExpandsInterNodeEdgesToSeparateSgBorderAndNodes3()
    {
        g.SetParent("sg2", "sg1");
        g.SetParent("a", "sg2");
        g.SetEdge("a", "b", new() { Minlen = 1 });
        NestingGraph.Run(g);
        await Assert.That(g.FindEdgeLabel("a", "b").Minlen).IsEqualTo(5);
    }

    [Test]
    public async Task SetsMinlenCorrectlyForNestedSgBorderToChildren()
    {
        g.SetParent("a", "sg1");
        g.SetParent("sg2", "sg1");
        g.SetParent("b", "sg2");
        NestingGraph.Run(g);

        // We expect the following layering:
        //
        // 0: root
        // 1: empty (close sg2)
        // 2: empty (close sg1)
        // 3: open sg1
        // 4: open sg2
        // 5: a, b
        // 6: close sg2
        // 7: close sg1

        var root = g.GraphLabel.NestingRoot!;
        var sg1Top = g.Node("sg1").BorderTop!;
        var sg1Bot = g.Node("sg1").BorderBottom!;
        var sg2Top = g.Node("sg2").BorderTop!;
        var sg2Bot = g.Node("sg2").BorderBottom!;

        await Assert.That(g.FindEdgeLabel(root, sg1Top).Minlen).IsEqualTo(3);
        await Assert.That(g.FindEdgeLabel(sg1Top, sg2Top).Minlen).IsEqualTo(1);
        await Assert.That(g.FindEdgeLabel(sg1Top, "a").Minlen).IsEqualTo(2);
        await Assert.That(g.FindEdgeLabel("a", sg1Bot).Minlen).IsEqualTo(2);
        await Assert.That(g.FindEdgeLabel(sg2Top, "b").Minlen).IsEqualTo(1);
        await Assert.That(g.FindEdgeLabel("b", sg2Bot).Minlen).IsEqualTo(1);
        await Assert.That(g.FindEdgeLabel(sg2Bot, sg1Bot).Minlen).IsEqualTo(1);
    }

    // ----- cleanup -----

    [Test]
    public async Task RemovesNestingGraphEdges()
    {
        g.SetParent("a", "sg1");
        g.SetEdge("a", "b", new() { Minlen = 1 });
        NestingGraph.Run(g);
        NestingGraph.Cleanup(g);
        await Assert.That(g.Successors("a")).IsEquivalentTo(new List<string> { "b" });
    }

    [Test]
    public async Task RemovesTheRootNode()
    {
        g.SetParent("a", "sg1");
        NestingGraph.Run(g);
        NestingGraph.Cleanup(g);
        await Assert.That(g.NodeCount).IsEqualTo(4); // sg1 + sg1Top + sg1Bottom + "a"
    }
}
