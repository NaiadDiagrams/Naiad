public class AcyclicTests
{
    static Graph NewGraph()
    {
        var graph = new Graph(multigraph: true);
        graph.SetGraph(new());
        graph.SetDefaultEdgeLabel((_, _, _) =>
            new()
            {
                Minlen = 1,
                Weight = 1
            });
        return graph;
    }

    // === run ===

    [Test]
    public async Task RunDoesNotChangeAnAlreadyAcyclicGraph()
    {
        var graph = NewGraph();
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
    public async Task RunBreaksCyclesInTheInputGraph()
    {
        var graph = NewGraph();
        graph.SetPath(["a", "b", "c", "d", "a"]);
        Acyclic.Run(graph);
        await Assert.That(GraphAlgorithms.FindCycles(graph)).IsEmpty();
    }

    [Test]
    public async Task RunCreatesAMultiEdgeWhereNecessary()
    {
        var graph = NewGraph();
        graph.SetPath(["a", "b", "a"]);
        Acyclic.Run(graph);
        await Assert.That(GraphAlgorithms.FindCycles(graph)).IsEmpty();
        if (graph.HasEdge("a", "b"))
        {
            await Assert.That(graph.OutEdges("a", "b")!.Count).IsEqualTo(2);
        }
        else
        {
            await Assert.That(graph.OutEdges("b", "a")!.Count).IsEqualTo(2);
        }

        await Assert.That(graph.Edges().Count).IsEqualTo(2);
    }

    // === undo ===

    [Test]
    public async Task UndoDoesNotChangeEdgesWhereTheOriginalGraphWasAcyclic()
    {
        var graph = NewGraph();
        graph.SetEdge("a", "b", new() { Minlen = 2, Weight = 3 });
        Acyclic.Run(graph);
        Acyclic.Undo(graph);
        var label = graph.FindEdgeLabel("a", "b");
        await Assert.That(label.Minlen).IsEqualTo(2);
        await Assert.That(label.Weight).IsEqualTo(3d);
        await Assert.That(graph.Edges().Count).IsEqualTo(1);
    }

    [Test]
    public async Task UndoCanRestorePreviouslyReversedEdges()
    {
        var graph = NewGraph();
        graph.SetEdge("a", "b", new() { Minlen = 2, Weight = 3 });
        graph.SetEdge("b", "a", new() { Minlen = 3, Weight = 4 });
        Acyclic.Run(graph);
        Acyclic.Undo(graph);
        var ab = graph.FindEdgeLabel("a", "b");
        await Assert.That(ab.Minlen).IsEqualTo(2);
        await Assert.That(ab.Weight).IsEqualTo(3d);
        var ba = graph.FindEdgeLabel("b", "a");
        await Assert.That(ba.Minlen).IsEqualTo(3);
        await Assert.That(ba.Weight).IsEqualTo(4d);
        await Assert.That(graph.Edges().Count).IsEqualTo(2);
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
