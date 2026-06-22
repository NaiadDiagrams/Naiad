namespace Naiad.Dagre.Tests;

public class CrossCountTests
{
    Graph graph = null!;

    [Before(Test)]
    public void Setup() =>
        graph = new Graph()
            .SetDefaultEdgeLabel((_, _, _) => new() { Weight = 1 });

    [Test]
    public async Task Returns0ForAnEmptyLayering()
    {
        await Assert.That(CrossCount.Run(graph, [])).IsEqualTo(0);
    }

    [Test]
    public async Task Returns0ForALayeringWithNoCrossings()
    {
        graph.SetEdge("a1", "b1");
        graph.SetEdge("a2", "b2");
        await Assert.That(CrossCount.Run(graph, [["a1", "a2"], ["b1", "b2"]])).IsEqualTo(0);
    }

    [Test]
    public async Task Returns1ForALayeringWith1Crossing()
    {
        graph.SetEdge("a1", "b1");
        graph.SetEdge("a2", "b2");
        await Assert.That(CrossCount.Run(graph, [["a1", "a2"], ["b2", "b1"]])).IsEqualTo(1);
    }

    [Test]
    public async Task ReturnsAWeightedCrossingCountForALayeringWith1Crossing()
    {
        graph.SetEdge("a1", "b1", new() { Weight = 2 });
        graph.SetEdge("a2", "b2", new() { Weight = 3 });
        await Assert.That(CrossCount.Run(graph, [["a1", "a2"], ["b2", "b1"]])).IsEqualTo(6);
    }

    [Test]
    public async Task CalculatesCrossingsAcrossLayers()
    {
        graph.SetPath(["a1", "b1", "c1"]);
        graph.SetPath(["a2", "b2", "c2"]);
        await Assert.That(CrossCount.Run(graph, [["a1", "a2"], ["b2", "b1"], ["c1", "c2"]])).IsEqualTo(2);
    }

    [Test]
    public async Task WorksForGraph1()
    {
        graph.SetPath(["a", "b", "c"]);
        graph.SetPath(["d", "e", "c"]);
        graph.SetPath(["a", "f", "i"]);
        graph.SetEdge("a", "e");
        await Assert.That(CrossCount.Run(graph, [["a", "d"], ["b", "e", "f"], ["c", "i"]])).IsEqualTo(1);
        await Assert.That(CrossCount.Run(graph, [["d", "a"], ["e", "b", "f"], ["c", "i"]])).IsEqualTo(0);
    }
}
