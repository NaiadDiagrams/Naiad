namespace Naiad.Dagre.Tests;

public class BarycenterTests
{
    Graph g = null!;

    [Before(Test)]
    public void Setup()
    {
        g = new Graph()
            .SetDefaultNodeLabel(_ => new())
            .SetDefaultEdgeLabel((_, _, _) => new() { Weight = 1 });
    }

    [Test]
    public async Task AssignsAnUndefinedBarycenterForANodeWithNoPredecessors()
    {
        g.SetNode("x", new());

        var results = Barycenter.Run(g, ["x"]);
        await Assert.That(results.Count).IsEqualTo(1);
        await Assert.That(results[0].V).IsEqualTo("x");
        await Assert.That(results[0].Barycenter).IsNull();
        await Assert.That(results[0].Weight).IsNull();
    }

    [Test]
    public async Task AssignsThePositionOfTheSolePredecessors()
    {
        g.SetNode("a", new() { Order = 2 });
        g.SetEdge("a", "x");

        var results = Barycenter.Run(g, ["x"]);
        await Assert.That(results.Count).IsEqualTo(1);
        await Assert.That(results[0].V).IsEqualTo("x");
        await Assert.That(results[0].Barycenter!.Value).IsEqualTo(2).Within(0.001);
        await Assert.That(results[0].Weight!.Value).IsEqualTo(1).Within(0.001);
    }

    [Test]
    public async Task AssignsTheAverageOfMultiplePredecessors()
    {
        g.SetNode("a", new() { Order = 2 });
        g.SetNode("b", new() { Order = 4 });
        g.SetEdge("a", "x");
        g.SetEdge("b", "x");

        var results = Barycenter.Run(g, ["x"]);
        await Assert.That(results.Count).IsEqualTo(1);
        await Assert.That(results[0].V).IsEqualTo("x");
        await Assert.That(results[0].Barycenter!.Value).IsEqualTo(3).Within(0.001);
        await Assert.That(results[0].Weight!.Value).IsEqualTo(2).Within(0.001);
    }

    [Test]
    public async Task TakesIntoAccountTheWeightOfEdges()
    {
        g.SetNode("a", new() { Order = 2 });
        g.SetNode("b", new() { Order = 4 });
        g.SetEdge("a", "x", new() { Weight = 3 });
        g.SetEdge("b", "x");

        var results = Barycenter.Run(g, ["x"]);
        await Assert.That(results.Count).IsEqualTo(1);
        await Assert.That(results[0].V).IsEqualTo("x");
        await Assert.That(results[0].Barycenter!.Value).IsEqualTo(2.5).Within(0.001);
        await Assert.That(results[0].Weight!.Value).IsEqualTo(4).Within(0.001);
    }

    [Test]
    public async Task CalculatesBarycentersForAllNodesInTheMovableLayer()
    {
        g.SetNode("a", new() { Order = 1 });
        g.SetNode("b", new() { Order = 2 });
        g.SetNode("c", new() { Order = 4 });
        g.SetEdge("a", "x");
        g.SetEdge("b", "x");
        g.SetNode("y");
        g.SetEdge("a", "z", new() { Weight = 2 });
        g.SetEdge("c", "z");

        var results = Barycenter.Run(g, ["x", "y", "z"]);
        await Assert.That(results.Count).IsEqualTo(3);

        await Assert.That(results[0].V).IsEqualTo("x");
        await Assert.That(results[0].Barycenter!.Value).IsEqualTo(1.5).Within(0.001);
        await Assert.That(results[0].Weight!.Value).IsEqualTo(2).Within(0.001);

        await Assert.That(results[1].V).IsEqualTo("y");
        await Assert.That(results[1].Barycenter).IsNull();
        await Assert.That(results[1].Weight).IsNull();

        await Assert.That(results[2].V).IsEqualTo("z");
        await Assert.That(results[2].Barycenter!.Value).IsEqualTo(2).Within(0.001);
        await Assert.That(results[2].Weight!.Value).IsEqualTo(3).Within(0.001);
    }
}
