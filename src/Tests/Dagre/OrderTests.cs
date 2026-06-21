namespace Naiad.Dagre.Tests;

public class OrderTests
{
    Graph g = null!;

    [Before(Test)]
    public void Setup() =>
        g = new Graph()
            .SetDefaultEdgeLabel(new EdgeLabel { Weight = 1 });

    [Test]
    public async Task DoesNotAddCrossingsToATreeStructure()
    {
        g.SetNode("a", new() { Rank = 1 });
        foreach (var v in new[] { "b", "e" })
        {
            g.SetNode(v, new() { Rank = 2 });
        }

        foreach (var v in new[] { "c", "d", "f" })
        {
            g.SetNode(v, new() { Rank = 3 });
        }

        g.SetPath(["a", "b", "c"]);
        g.SetEdge("b", "d");
        g.SetPath(["a", "e", "f"]);
        Order.Run(g);
        var layering = Util.BuildLayerMatrix(g);
        await Assert.That(CrossCount.Run(g, layering)).IsEqualTo(0);
    }

    [Test]
    public async Task CanSolveASimpleGraph()
    {
        // This graph resulted in a single crossing for previous versions of dagre.
        foreach (var v in new[] { "a", "d" })
        {
            g.SetNode(v, new() { Rank = 1 });
        }

        foreach (var v in new[] { "b", "f", "e" })
        {
            g.SetNode(v, new() { Rank = 2 });
        }

        foreach (var v in new[] { "c", "g" })
        {
            g.SetNode(v, new() { Rank = 3 });
        }

        Order.Run(g);
        var layering = Util.BuildLayerMatrix(g);
        await Assert.That(CrossCount.Run(g, layering)).IsEqualTo(0);
    }

    [Test]
    public async Task CanMinimizeCrossings()
    {
        g.SetNode("a", new() { Rank = 1 });
        foreach (var v in new[] { "b", "e", "g" })
        {
            g.SetNode(v, new() { Rank = 2 });
        }

        foreach (var v in new[] { "c", "f", "h" })
        {
            g.SetNode(v, new() { Rank = 3 });
        }

        g.SetNode("d", new() { Rank = 4 });
        Order.Run(g);
        var layering = Util.BuildLayerMatrix(g);
        await Assert.That(CrossCount.Run(g, layering)).IsLessThanOrEqualTo(1);
    }

    [Test]
    public async Task CanSkipTheOptimalOrdering()
    {
        g.SetNode("a", new() { Rank = 1 });
        foreach (var v in new[] { "b", "d" })
        {
            g.SetNode(v, new() { Rank = 2 });
        }

        foreach (var v in new[] { "c", "e" })
        {
            g.SetNode(v, new() { Rank = 3 });
        }

        g.SetPath(["a", "b", "c"]);
        g.SetPath(["a", "d"]);
        g.SetEdge("b", "e");
        g.SetEdge("d", "c");

        var opts = new OrderOptions { DisableOptimalOrderHeuristic = true };

        Order.Run(g, opts);
        var layering = Util.BuildLayerMatrix(g);
        await Assert.That(CrossCount.Run(g, layering)).IsEqualTo(1);
    }
}
