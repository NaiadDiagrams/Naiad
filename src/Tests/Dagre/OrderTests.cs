namespace Naiad.Dagre.Tests;

public class OrderTests
{
    Graph graph = null!;

    [Before(Test)]
    public void Setup() =>
        graph = new Graph()
            .SetDefaultEdgeLabel(new EdgeLabel { Weight = 1 });

    [Test]
    public async Task DoesNotAddCrossingsToATreeStructure()
    {
        graph.SetNode("a", new() { Rank = 1 });
        foreach (var v in new[] { "b", "e" })
        {
            graph.SetNode(v, new() { Rank = 2 });
        }

        foreach (var v in new[] { "c", "d", "f" })
        {
            graph.SetNode(v, new() { Rank = 3 });
        }

        graph.SetPath(["a", "b", "c"]);
        graph.SetEdge("b", "d");
        graph.SetPath(["a", "e", "f"]);
        Order.Run(graph);
        var layering = Util.BuildLayerMatrix(graph);
        await Assert.That(CrossCount.Run(graph, layering)).IsEqualTo(0);
    }

    [Test]
    public async Task CanSolveASimpleGraph()
    {
        // This graph resulted in a single crossing for previous versions of dagre.
        foreach (var v in new[] { "a", "d" })
        {
            graph.SetNode(v, new() { Rank = 1 });
        }

        foreach (var v in new[] { "b", "f", "e" })
        {
            graph.SetNode(v, new() { Rank = 2 });
        }

        foreach (var v in new[] { "c", "graph" })
        {
            graph.SetNode(v, new() { Rank = 3 });
        }

        Order.Run(graph);
        var layering = Util.BuildLayerMatrix(graph);
        await Assert.That(CrossCount.Run(graph, layering)).IsEqualTo(0);
    }

    [Test]
    public async Task CanMinimizeCrossings()
    {
        graph.SetNode("a", new() { Rank = 1 });
        foreach (var v in new[] { "b", "e", "graph" })
        {
            graph.SetNode(v, new() { Rank = 2 });
        }

        foreach (var v in new[] { "c", "f", "h" })
        {
            graph.SetNode(v, new() { Rank = 3 });
        }

        graph.SetNode("d", new() { Rank = 4 });
        Order.Run(graph);
        var layering = Util.BuildLayerMatrix(graph);
        await Assert.That(CrossCount.Run(graph, layering)).IsLessThanOrEqualTo(1);
    }

    [Test]
    public async Task CanSkipTheOptimalOrdering()
    {
        graph.SetNode("a", new() { Rank = 1 });
        foreach (var v in new[] { "b", "d" })
        {
            graph.SetNode(v, new() { Rank = 2 });
        }

        foreach (var v in new[] { "c", "e" })
        {
            graph.SetNode(v, new() { Rank = 3 });
        }

        graph.SetPath(["a", "b", "c"]);
        graph.SetPath(["a", "d"]);
        graph.SetEdge("b", "e");
        graph.SetEdge("d", "c");

        var opts = new OrderOptions { DisableOptimalOrderHeuristic = true };

        Order.Run(graph, opts);
        var layering = Util.BuildLayerMatrix(graph);
        await Assert.That(CrossCount.Run(graph, layering)).IsEqualTo(1);
    }
}
