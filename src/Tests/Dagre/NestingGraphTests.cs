public class NestingGraphTests
{
    Graph graph = null!;

    [Before(Test)]
    public void Setup() =>
        graph = new Graph(compound: true)
            .SetGraph(new())
            .SetDefaultNodeLabel(_ => new());

    // ----- run -----

    [Test]
    public async Task ConnectsADisconnectedGraph()
    {
        graph.SetNode("a");
        graph.SetNode("b");
        await Assert.That(GraphAlgorithms.Components(graph).Count).IsEqualTo(2);
        NestingGraph.Run(graph);
        await Assert.That(GraphAlgorithms.Components(graph).Count).IsEqualTo(1);
        await Assert.That(graph.HasNode("a")).IsTrue();
        await Assert.That(graph.HasNode("b")).IsTrue();
    }

    [Test]
    public async Task AddsBorderNodesToTheTopAndBottomOfASubgraph()
    {
        graph.SetParent("a", "sg1");
        NestingGraph.Run(graph);

        var borderTop = graph.NodeLabel("sg1").BorderTop;
        var borderBottom = graph.NodeLabel("sg1").BorderBottom;
        await Assert.That(borderTop).IsNotNull();
        await Assert.That(borderBottom).IsNotNull();
        await Assert.That(graph.Parent(borderTop!)).IsEqualTo("sg1");
        await Assert.That(graph.Parent(borderBottom!)).IsEqualTo("sg1");

        var topToA = graph.OutEdges(borderTop!, "a")!;
        await Assert.That(topToA.Count).IsEqualTo(1);
        await Assert.That(graph.FindEdgeLabel(topToA[0]).Minlen).IsEqualTo(1);

        var aToBottom = graph.OutEdges("a", borderBottom!)!;
        await Assert.That(aToBottom.Count).IsEqualTo(1);
        await Assert.That(graph.FindEdgeLabel(aToBottom[0]).Minlen).IsEqualTo(1);

        var topNode = graph.NodeLabel(borderTop!);
        await Assert.That(topNode.Width).IsEqualTo(0);
        await Assert.That(topNode.Height).IsEqualTo(0);
        await Assert.That(topNode.Dummy).IsEqualTo(DummyKind.Border);

        var bottomNode = graph.NodeLabel(borderBottom!);
        await Assert.That(bottomNode.Width).IsEqualTo(0);
        await Assert.That(bottomNode.Height).IsEqualTo(0);
        await Assert.That(bottomNode.Dummy).IsEqualTo(DummyKind.Border);
    }

    [Test]
    public async Task AddsEdgesBetweenBordersOfNestedSubgraphs()
    {
        graph.SetParent("sg2", "sg1");
        graph.SetParent("a", "sg2");
        NestingGraph.Run(graph);

        var sg1Top = graph.NodeLabel("sg1").BorderTop;
        var sg1Bottom = graph.NodeLabel("sg1").BorderBottom;
        var sg2Top = graph.NodeLabel("sg2").BorderTop;
        var sg2Bottom = graph.NodeLabel("sg2").BorderBottom;
        await Assert.That(sg1Top).IsNotNull();
        await Assert.That(sg1Bottom).IsNotNull();
        await Assert.That(sg2Top).IsNotNull();
        await Assert.That(sg2Bottom).IsNotNull();

        var sg1TopToSg2Top = graph.OutEdges(sg1Top!, sg2Top!)!;
        await Assert.That(sg1TopToSg2Top.Count).IsEqualTo(1);
        await Assert.That(graph.FindEdgeLabel(sg1TopToSg2Top[0]).Minlen).IsEqualTo(1);

        var sg2BottomToSg1Bottom = graph.OutEdges(sg2Bottom!, sg1Bottom!)!;
        await Assert.That(sg2BottomToSg1Bottom.Count).IsEqualTo(1);
        await Assert.That(graph.FindEdgeLabel(sg2BottomToSg1Bottom[0]).Minlen).IsEqualTo(1);
    }

    [Test]
    public async Task AddsSufficientWeightToBorderToNodeEdges()
    {
        // We want to keep subgraphs tight, so we should ensure that the weight for
        // the edge between the top (and bottom) border nodes and nodes in the
        // subgraph have weights exceeding anything in the graph.
        graph.SetParent("x", "sg");
        graph.SetEdge("a", "x", new() { Weight = 100 });
        graph.SetEdge("x", "b", new() { Weight = 200 });
        NestingGraph.Run(graph);

        var top = graph.NodeLabel("sg").BorderTop;
        var bot = graph.NodeLabel("sg").BorderBottom;
        await Assert.That(graph.FindEdgeLabel(top!, "x").Weight!.Value).IsGreaterThan(300);
        await Assert.That(graph.FindEdgeLabel("x", bot!).Weight!.Value).IsGreaterThan(300);
    }

    [Test]
    public async Task AddsAnEdgeFromTheRootToTheTopsOfTopLevelSubgraphs()
    {
        graph.SetParent("a", "sg1");
        NestingGraph.Run(graph);

        var root = graph.Label.NestingRoot;
        var borderTop = graph.NodeLabel("sg1").BorderTop;
        await Assert.That(root).IsNotNull();
        await Assert.That(borderTop).IsNotNull();

        var rootToTop = graph.OutEdges(root!, borderTop!)!;
        await Assert.That(rootToTop.Count).IsEqualTo(1);
        await Assert.That(graph.HasEdge(rootToTop[0].V, rootToTop[0].W, rootToTop[0].Name)).IsTrue();
    }

    [Test]
    public async Task AddsAnEdgeFromRootToEachNodeWithTheCorrectMinlen1()
    {
        graph.SetNode("a");
        NestingGraph.Run(graph);

        var root = graph.Label.NestingRoot;
        await Assert.That(root).IsNotNull();

        var rootToA = graph.OutEdges(root!, "a")!;
        await Assert.That(rootToA.Count).IsEqualTo(1);

        var label = graph.FindEdgeLabel(rootToA[0]);
        await Assert.That(label.Weight).IsEqualTo(0);
        await Assert.That(label.Minlen).IsEqualTo(1);
    }

    [Test]
    public async Task AddsAnEdgeFromRootToEachNodeWithTheCorrectMinlen2()
    {
        graph.SetParent("a", "sg1");
        NestingGraph.Run(graph);

        var root = graph.Label.NestingRoot;
        await Assert.That(root).IsNotNull();

        var rootToA = graph.OutEdges(root!, "a")!;
        await Assert.That(rootToA.Count).IsEqualTo(1);

        var label = graph.FindEdgeLabel(rootToA[0]);
        await Assert.That(label.Weight).IsEqualTo(0);
        await Assert.That(label.Minlen).IsEqualTo(3);
    }

    [Test]
    public async Task AddsAnEdgeFromRootToEachNodeWithTheCorrectMinlen3()
    {
        graph.SetParent("sg2", "sg1");
        graph.SetParent("a", "sg2");
        NestingGraph.Run(graph);

        var root = graph.Label.NestingRoot;
        await Assert.That(root).IsNotNull();

        var rootToA = graph.OutEdges(root!, "a")!;
        await Assert.That(rootToA.Count).IsEqualTo(1);

        var label = graph.FindEdgeLabel(rootToA[0]);
        await Assert.That(label.Weight).IsEqualTo(0);
        await Assert.That(label.Minlen).IsEqualTo(5);
    }

    [Test]
    public async Task DoesNotAddAnEdgeFromTheRootToItself()
    {
        graph.SetNode("a");
        NestingGraph.Run(graph);

        var root = graph.Label.NestingRoot;
        var rootToRoot = graph.OutEdges(root!, root!)!;
        await Assert.That(rootToRoot.Count).IsEqualTo(0);
    }

    [Test]
    public async Task ExpandsInterNodeEdgesToSeparateSgBorderAndNodes1()
    {
        graph.SetEdge("a", "b", new() { Minlen = 1 });
        NestingGraph.Run(graph);
        await Assert.That(graph.FindEdgeLabel("a", "b").Minlen).IsEqualTo(1);
    }

    [Test]
    public async Task ExpandsInterNodeEdgesToSeparateSgBorderAndNodes2()
    {
        graph.SetParent("a", "sg1");
        graph.SetEdge("a", "b", new() { Minlen = 1 });
        NestingGraph.Run(graph);
        await Assert.That(graph.FindEdgeLabel("a", "b").Minlen).IsEqualTo(3);
    }

    [Test]
    public async Task ExpandsInterNodeEdgesToSeparateSgBorderAndNodes3()
    {
        graph.SetParent("sg2", "sg1");
        graph.SetParent("a", "sg2");
        graph.SetEdge("a", "b", new() { Minlen = 1 });
        NestingGraph.Run(graph);
        await Assert.That(graph.FindEdgeLabel("a", "b").Minlen).IsEqualTo(5);
    }

    [Test]
    public async Task SetsMinlenCorrectlyForNestedSgBorderToChildren()
    {
        graph.SetParent("a", "sg1");
        graph.SetParent("sg2", "sg1");
        graph.SetParent("b", "sg2");
        NestingGraph.Run(graph);

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

        var root = graph.Label.NestingRoot!;
        var sg1Top = graph.NodeLabel("sg1").BorderTop!;
        var sg1Bot = graph.NodeLabel("sg1").BorderBottom!;
        var sg2Top = graph.NodeLabel("sg2").BorderTop!;
        var sg2Bot = graph.NodeLabel("sg2").BorderBottom!;

        await Assert.That(graph.FindEdgeLabel(root, sg1Top).Minlen).IsEqualTo(3);
        await Assert.That(graph.FindEdgeLabel(sg1Top, sg2Top).Minlen).IsEqualTo(1);
        await Assert.That(graph.FindEdgeLabel(sg1Top, "a").Minlen).IsEqualTo(2);
        await Assert.That(graph.FindEdgeLabel("a", sg1Bot).Minlen).IsEqualTo(2);
        await Assert.That(graph.FindEdgeLabel(sg2Top, "b").Minlen).IsEqualTo(1);
        await Assert.That(graph.FindEdgeLabel("b", sg2Bot).Minlen).IsEqualTo(1);
        await Assert.That(graph.FindEdgeLabel(sg2Bot, sg1Bot).Minlen).IsEqualTo(1);
    }

    // ----- cleanup -----

    [Test]
    public async Task RemovesNestingGraphEdges()
    {
        graph.SetParent("a", "sg1");
        graph.SetEdge("a", "b", new() { Minlen = 1 });
        NestingGraph.Run(graph);
        NestingGraph.Cleanup(graph);
        await Assert.That(graph.Successors("a")).IsEquivalentTo(new List<string> { "b" });
    }

    [Test]
    public async Task RemovesTheRootNode()
    {
        graph.SetParent("a", "sg1");
        NestingGraph.Run(graph);
        NestingGraph.Cleanup(graph);
        await Assert.That(graph.NodeCount).IsEqualTo(4); // sg1 + sg1Top + sg1Bottom + "a"
    }
}
