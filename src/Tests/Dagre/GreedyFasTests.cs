namespace Naiad.Dagre.Tests;

public class GreedyFasTests
{
    [Test]
    public async Task ReturnsTheEmptySetForEmptyGraphs()
    {
        var graph = new Graph();
        await Assert.That(GreedyFas.Run(graph)).IsEmpty();
    }

    [Test]
    public async Task ReturnsTheEmptySetForSingleNodeGraphs()
    {
        var graph = new Graph();
        graph.SetNode("a");
        await Assert.That(GreedyFas.Run(graph)).IsEmpty();
    }

    [Test]
    public async Task ReturnsAnEmptySetIfTheInputGraphIsAcyclic()
    {
        var graph = new Graph();
        graph.SetDefaultEdgeLabel((_, _, _) => new() { Weight = 1 });
        graph.SetEdge("a", "b");
        graph.SetEdge("b", "c");
        graph.SetEdge("b", "d");
        graph.SetEdge("a", "e");
        await Assert.That(GreedyFas.Run(graph)).IsEmpty();
    }

    [Test]
    public Task ReturnsASingleEdgeWithASimpleCycle()
    {
        var graph = new Graph();
        graph.SetDefaultEdgeLabel((_, _, _) => new() { Weight = 1 });
        graph.SetEdge("a", "b");
        graph.SetEdge("b", "a");
        return CheckFas(graph, GreedyFas.Run(graph));
    }

    [Test]
    public Task ReturnsASingleEdgeInA4NodeCycle()
    {
        var graph = new Graph();
        graph.SetDefaultEdgeLabel((_, _, _) => new() { Weight = 1 });
        graph.SetEdge("n1", "n2");
        graph.SetPath(["n2", "n3", "n4", "n5", "n2"]);
        graph.SetEdge("n3", "n5");
        graph.SetEdge("n4", "n2");
        graph.SetEdge("n4", "n6");
        return CheckFas(graph, GreedyFas.Run(graph));
    }

    [Test]
    public Task ReturnsTwoEdgesForTwo4NodeCycles()
    {
        var graph = new Graph();
        graph.SetDefaultEdgeLabel((_, _, _) => new() { Weight = 1 });
        graph.SetEdge("n1", "n2");
        graph.SetPath(["n2", "n3", "n4", "n5", "n2"]);
        graph.SetEdge("n3", "n5");
        graph.SetEdge("n4", "n2");
        graph.SetEdge("n4", "n6");
        graph.SetPath(["n6", "n7", "n8", "n9", "n6"]);
        graph.SetEdge("n7", "n9");
        graph.SetEdge("n8", "n6");
        graph.SetEdge("n8", "n10");
        return CheckFas(graph, GreedyFas.Run(graph));
    }

    [Test]
    public async Task WorksWithArbitrarilyWeightedEdges()
    {
        // Our algorithm should also work for graphs with multi-edges, a graph
        // where more than one edge can be pointing in the same direction between
        // the same pair of incident nodes. We try this by assigning weights to
        // our edges representing the number of edges from one node to the other.

        var g1 = new Graph();
        g1.SetEdge("n1", "n2", new() { Weight = 2 });
        g1.SetEdge("n2", "n1", new() { Weight = 1 });
        await Assert.That(GreedyFas.Run(g1, WeightFn(g1)))
            .IsEquivalentTo(new List<Edge> { new("n2", "n1") });

        var g2 = new Graph();
        g2.SetEdge("n1", "n2", new() { Weight = 1 });
        g2.SetEdge("n2", "n1", new() { Weight = 2 });
        await Assert.That(GreedyFas.Run(g2, WeightFn(g2)))
            .IsEquivalentTo(new List<Edge> { new("n1", "n2") });
    }

    [Test]
    public async Task WorksForMultigraphs()
    {
        var graph = new Graph(multigraph: true);
        graph.SetEdge("a", "b", new() { Weight = 5 }, "foo");
        graph.SetEdge("b", "a", new() { Weight = 2 }, "bar");
        graph.SetEdge("b", "a", new() { Weight = 2 }, "baz");
        var result = GreedyFas.Run(graph, WeightFn(graph));
        result.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
        var expected = new List<Edge>
        {
            new("b", "a", "bar"),
            new("b", "a", "baz")
        };
        await Assert.That(result).IsEquivalentTo(expected);
    }

    static async Task CheckFas(Graph graph, List<Edge> fas)
    {
        var n = graph.NodeCount;
        var m = graph.EdgeCount;
        foreach (var edge in fas)
        {
            graph.RemoveEdge(edge.V, edge.W);
        }

        await Assert.That(Alg.FindCycles(graph)).IsEmpty();
        // The more direct m/2 - n/6 fails for the simple cycle A <-> B, where one
        // edge must be reversed, but the performance bound implies that only 2/3rds
        // of an edge can be reversed. I'm using floors to acount for this.
        await Assert.That(fas.Count).IsLessThanOrEqualTo((int)Math.Floor(m / 2.0) - (int)Math.Floor(n / 6.0));
    }

    static Func<Edge, double> WeightFn(Graph graph) =>
        e => graph.FindEdgeLabel(e).Weight!.Value;
}
