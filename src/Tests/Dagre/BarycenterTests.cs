namespace Naiad.Dagre.Tests;

public class BarycenterTests
{
    Graph graph = null!;

    [Before(Test)]
    public void Setup() =>
        graph = new Graph()
            .SetDefaultNodeLabel(_ => new())
            .SetDefaultEdgeLabel((_, _, _) => new() { Weight = 1 });

    [Test]
    public async Task AssignsAnUndefinedBarycenterForANodeWithNoPredecessors()
    {
        graph.SetNode("x", new());

        var results = Barycenter.Run(graph, ["x"]);
        await Assert.That(results.Count).IsEqualTo(1);
        await Assert.That(results[0].V).IsEqualTo("x");
        await Assert.That(results[0].Barycenter).IsNull();
        await Assert.That(results[0].Weight).IsNull();
    }

    [Test]
    public async Task AssignsThePositionOfTheSolePredecessors()
    {
        graph.SetNode("a", new() { Order = 2 });
        graph.SetEdge("a", "x");

        var results = Barycenter.Run(graph, ["x"]);
        await Assert.That(results.Count).IsEqualTo(1);
        await Assert.That(results[0].V).IsEqualTo("x");
        await Assert.That(results[0].Barycenter!.Value).IsEqualTo(2).Within(0.001);
        await Assert.That(results[0].Weight!.Value).IsEqualTo(1).Within(0.001);
    }

    [Test]
    public async Task AssignsTheAverageOfMultiplePredecessors()
    {
        graph.SetNode("a", new() { Order = 2 });
        graph.SetNode("b", new() { Order = 4 });
        graph.SetEdge("a", "x");
        graph.SetEdge("b", "x");

        var results = Barycenter.Run(graph, ["x"]);
        await Assert.That(results.Count).IsEqualTo(1);
        await Assert.That(results[0].V).IsEqualTo("x");
        await Assert.That(results[0].Barycenter!.Value).IsEqualTo(3).Within(0.001);
        await Assert.That(results[0].Weight!.Value).IsEqualTo(2).Within(0.001);
    }

    [Test]
    public async Task TakesIntoAccountTheWeightOfEdges()
    {
        graph.SetNode("a", new() { Order = 2 });
        graph.SetNode("b", new() { Order = 4 });
        graph.SetEdge("a", "x", new() { Weight = 3 });
        graph.SetEdge("b", "x");

        var results = Barycenter.Run(graph, ["x"]);
        await Assert.That(results.Count).IsEqualTo(1);
        await Assert.That(results[0].V).IsEqualTo("x");
        await Assert.That(results[0].Barycenter!.Value).IsEqualTo(2.5).Within(0.001);
        await Assert.That(results[0].Weight!.Value).IsEqualTo(4).Within(0.001);
    }

    [Test]
    public async Task CalculatesBarycentersForAllNodesInTheMovableLayer()
    {
        graph.SetNode("a", new() { Order = 1 });
        graph.SetNode("b", new() { Order = 2 });
        graph.SetNode("c", new() { Order = 4 });
        graph.SetEdge("a", "x");
        graph.SetEdge("b", "x");
        graph.SetNode("y");
        graph.SetEdge("a", "z", new() { Weight = 2 });
        graph.SetEdge("c", "z");

        var results = Barycenter.Run(graph, ["x", "y", "z"]);
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
