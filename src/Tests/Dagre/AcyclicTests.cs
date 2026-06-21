public class AcyclicTests
{
    public static IEnumerable<string> Acyclicers()
    {
        yield return "greedy";
        yield return "dfs";
        yield return "unknown-should-still-work";
    }

    static Graph NewGraph() =>
        new Graph(multigraph: true)
            .SetGraph(new())
            .SetDefaultEdgeLabel((_, _, _) =>
                new()
                {
                    Minlen = 1,
                    Weight = 1
                });

    // === run ===

    [Test]
    [MethodDataSource(nameof(Acyclicers))]
    public async Task RunDoesNotChangeAnAlreadyAcyclicGraph(string acyclicer)
    {
        var graph = NewGraph();
        graph.Graph_().Acyclicer = acyclicer;
        graph.SetPath(["a", "b", "d"]);
        graph.SetPath(["a", "c", "d"]);
        Acyclic.Run(graph);
        var results = graph.Edges().Select(StripLabel).ToList();
        results.Sort(SortEdges);
        var expected = new List<Edge>
        {
            new("a", "b"),
            new("a", "c"),
            new("b", "d"),
            new("c", "d")
        };
        await Assert.That(results).IsEquivalentTo(expected);
    }

    [Test]
    [MethodDataSource(nameof(Acyclicers))]
    public async Task RunBreaksCyclesInTheInputGraph(string acyclicer)
    {
        var graph = NewGraph();
        graph.Graph_().Acyclicer = acyclicer;
        graph.SetPath(["a", "b", "c", "d", "a"]);
        Acyclic.Run(graph);
        await Assert.That(Alg.FindCycles(graph)).IsEmpty();
    }

    [Test]
    [MethodDataSource(nameof(Acyclicers))]
    public async Task RunCreatesAMultiEdgeWhereNecessary(string acyclicer)
    {
        var graph = NewGraph();
        graph.Graph_().Acyclicer = acyclicer;
        graph.SetPath(["a", "b", "a"]);
        Acyclic.Run(graph);
        await Assert.That(Alg.FindCycles(graph)).IsEmpty();
        if (graph.HasEdge("a", "b"))
        {
            await Assert.That(graph.OutEdges("a", "b")!.Count).IsEqualTo(2);
        }
        else
        {
            await Assert.That(graph.OutEdges("b", "a")!.Count).IsEqualTo(2);
        }

        await Assert.That(graph.EdgeCount()).IsEqualTo(2);
    }

    // === undo ===

    [Test]
    [MethodDataSource(nameof(Acyclicers))]
    public async Task UndoDoesNotChangeEdgesWhereTheOriginalGraphWasAcyclic(string acyclicer)
    {
        var graph = NewGraph();
        graph.Graph_().Acyclicer = acyclicer;
        graph.SetEdge("a", "b", new() { Minlen = 2, Weight = 3 });
        Acyclic.Run(graph);
        Acyclic.Undo(graph);
        var label = graph.Edge_("a", "b");
        await Assert.That(label.Minlen).IsEqualTo(2);
        await Assert.That(label.Weight).IsEqualTo(3d);
        await Assert.That(graph.Edges().Count).IsEqualTo(1);
    }

    [Test]
    [MethodDataSource(nameof(Acyclicers))]
    public async Task UndoCanRestorePreviouslyReversedEdges(string acyclicer)
    {
        var graph = NewGraph();
        graph.Graph_().Acyclicer = acyclicer;
        graph.SetEdge("a", "b", new() { Minlen = 2, Weight = 3 });
        graph.SetEdge("b", "a", new() { Minlen = 3, Weight = 4 });
        Acyclic.Run(graph);
        Acyclic.Undo(graph);
        var ab = graph.Edge_("a", "b");
        await Assert.That(ab.Minlen).IsEqualTo(2);
        await Assert.That(ab.Weight).IsEqualTo(3d);
        var ba = graph.Edge_("b", "a");
        await Assert.That(ba.Minlen).IsEqualTo(3);
        await Assert.That(ba.Weight).IsEqualTo(4d);
        await Assert.That(graph.Edges().Count).IsEqualTo(2);
    }

    // === greedy-specific functionality ===

    [Test]
    public async Task GreedyPrefersToBreakCyclesAtLowWeightEdges()
    {
        var g = NewGraph();
        g.Graph_().Acyclicer = "greedy";
        g.SetDefaultEdgeLabel((_, _, _) => new() { Minlen = 1, Weight = 2 });
        g.SetPath(["a", "b", "c", "d", "a"]);
        g.SetEdge("c", "d", new() { Weight = 1 });
        Acyclic.Run(g);
        await Assert.That(Alg.FindCycles(g)).IsEmpty();
        await Assert.That(g.HasEdge("c", "d")).IsFalse();
    }

    static Edge StripLabel(Edge edge) =>
        // The TS test strips the "label" property; the C# Edge record already carries only {V, W, Name}.
        new(edge.V, edge.W, edge.Name);

    static int SortEdges(Edge a, Edge b)
    {
        if (a.Name != null && b.Name != null)
        {
            return string.CompareOrdinal(a.Name, b.Name);
        }

        var order = string.CompareOrdinal(a.V, b.V);
        if (order != 0)
        {
            return order;
        }

        return string.CompareOrdinal(a.W, b.W);
    }
}
