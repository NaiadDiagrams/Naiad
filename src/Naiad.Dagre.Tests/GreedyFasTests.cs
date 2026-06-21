namespace Naiad.Dagre.Tests;

public class GreedyFasTests
{
    [Test]
    public async Task ReturnsTheEmptySetForEmptyGraphs()
    {
        var g = new Graph();
        await Assert.That(GreedyFas.Run(g)).IsEmpty();
    }

    [Test]
    public async Task ReturnsTheEmptySetForSingleNodeGraphs()
    {
        var g = new Graph();
        g.SetNode("a");
        await Assert.That(GreedyFas.Run(g)).IsEmpty();
    }

    [Test]
    public async Task ReturnsAnEmptySetIfTheInputGraphIsAcyclic()
    {
        var g = new Graph();
        g.SetDefaultEdgeLabel((_, _, _) => new EdgeLabel { Weight = 1 });
        g.SetEdge("a", "b");
        g.SetEdge("b", "c");
        g.SetEdge("b", "d");
        g.SetEdge("a", "e");
        await Assert.That(GreedyFas.Run(g)).IsEmpty();
    }

    [Test]
    public async Task ReturnsASingleEdgeWithASimpleCycle()
    {
        var g = new Graph();
        g.SetDefaultEdgeLabel((_, _, _) => new EdgeLabel { Weight = 1 });
        g.SetEdge("a", "b");
        g.SetEdge("b", "a");
        await CheckFas(g, GreedyFas.Run(g));
    }

    [Test]
    public async Task ReturnsASingleEdgeInA4NodeCycle()
    {
        var g = new Graph();
        g.SetDefaultEdgeLabel((_, _, _) => new EdgeLabel { Weight = 1 });
        g.SetEdge("n1", "n2");
        g.SetPath(["n2", "n3", "n4", "n5", "n2"]);
        g.SetEdge("n3", "n5");
        g.SetEdge("n4", "n2");
        g.SetEdge("n4", "n6");
        await CheckFas(g, GreedyFas.Run(g));
    }

    [Test]
    public async Task ReturnsTwoEdgesForTwo4NodeCycles()
    {
        var g = new Graph();
        g.SetDefaultEdgeLabel((_, _, _) => new EdgeLabel { Weight = 1 });
        g.SetEdge("n1", "n2");
        g.SetPath(["n2", "n3", "n4", "n5", "n2"]);
        g.SetEdge("n3", "n5");
        g.SetEdge("n4", "n2");
        g.SetEdge("n4", "n6");
        g.SetPath(["n6", "n7", "n8", "n9", "n6"]);
        g.SetEdge("n7", "n9");
        g.SetEdge("n8", "n6");
        g.SetEdge("n8", "n10");
        await CheckFas(g, GreedyFas.Run(g));
    }

    [Test]
    public async Task WorksWithArbitrarilyWeightedEdges()
    {
        // Our algorithm should also work for graphs with multi-edges, a graph
        // where more than one edge can be pointing in the same direction between
        // the same pair of incident nodes. We try this by assigning weights to
        // our edges representing the number of edges from one node to the other.

        var g1 = new Graph();
        g1.SetEdge("n1", "n2", new EdgeLabel { Weight = 2 });
        g1.SetEdge("n2", "n1", new EdgeLabel { Weight = 1 });
        await Assert.That(GreedyFas.Run(g1, WeightFn(g1)))
            .IsEquivalentTo(new List<Edge> { new("n2", "n1") });

        var g2 = new Graph();
        g2.SetEdge("n1", "n2", new EdgeLabel { Weight = 1 });
        g2.SetEdge("n2", "n1", new EdgeLabel { Weight = 2 });
        await Assert.That(GreedyFas.Run(g2, WeightFn(g2)))
            .IsEquivalentTo(new List<Edge> { new("n1", "n2") });
    }

    [Test]
    public async Task WorksForMultigraphs()
    {
        var g = new Graph(multigraph: true);
        g.SetEdge("a", "b", new EdgeLabel { Weight = 5 }, "foo");
        g.SetEdge("b", "a", new EdgeLabel { Weight = 2 }, "bar");
        g.SetEdge("b", "a", new EdgeLabel { Weight = 2 }, "baz");
        var result = GreedyFas.Run(g, WeightFn(g));
        result.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
        var expected = new List<Edge>
        {
            new("b", "a", "bar"),
            new("b", "a", "baz")
        };
        await Assert.That(result).IsEquivalentTo(expected);
    }

    static async Task CheckFas(Graph g, List<Edge> fas)
    {
        var n = g.NodeCount();
        var m = g.EdgeCount();
        foreach (var edge in fas)
        {
            g.RemoveEdge(edge.V, edge.W);
        }

        await Assert.That(Alg.FindCycles(g)).IsEmpty();
        // The more direct m/2 - n/6 fails for the simple cycle A <-> B, where one
        // edge must be reversed, but the performance bound implies that only 2/3rds
        // of an edge can be reversed. I'm using floors to acount for this.
        await Assert.That(fas.Count).IsLessThanOrEqualTo((int)Math.Floor(m / 2.0) - (int)Math.Floor(n / 6.0));
    }

    static Func<Edge, double> WeightFn(Graph g) =>
        e => g.Edge_(e).Weight!.Value;
}
