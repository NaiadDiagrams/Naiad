namespace Naiad.Dagre.Tests;

public class CrossCountTests
{
    Graph g = null!;

    [Before(Test)]
    public void Setup() =>
        g = new Graph()
            .SetDefaultEdgeLabel((_, _, _) => new() { Weight = 1 });

    [Test]
    public async Task Returns0ForAnEmptyLayering()
    {
        await Assert.That(CrossCount.Run(g, [])).IsEqualTo(0);
    }

    [Test]
    public async Task Returns0ForALayeringWithNoCrossings()
    {
        g.SetEdge("a1", "b1");
        g.SetEdge("a2", "b2");
        await Assert.That(CrossCount.Run(g, [["a1", "a2"], ["b1", "b2"]])).IsEqualTo(0);
    }

    [Test]
    public async Task Returns1ForALayeringWith1Crossing()
    {
        g.SetEdge("a1", "b1");
        g.SetEdge("a2", "b2");
        await Assert.That(CrossCount.Run(g, [["a1", "a2"], ["b2", "b1"]])).IsEqualTo(1);
    }

    [Test]
    public async Task ReturnsAWeightedCrossingCountForALayeringWith1Crossing()
    {
        g.SetEdge("a1", "b1", new() { Weight = 2 });
        g.SetEdge("a2", "b2", new() { Weight = 3 });
        await Assert.That(CrossCount.Run(g, [["a1", "a2"], ["b2", "b1"]])).IsEqualTo(6);
    }

    [Test]
    public async Task CalculatesCrossingsAcrossLayers()
    {
        g.SetPath(["a1", "b1", "c1"]);
        g.SetPath(["a2", "b2", "c2"]);
        await Assert.That(CrossCount.Run(g, [["a1", "a2"], ["b2", "b1"], ["c1", "c2"]])).IsEqualTo(2);
    }

    [Test]
    public async Task WorksForGraph1()
    {
        g.SetPath(["a", "b", "c"]);
        g.SetPath(["d", "e", "c"]);
        g.SetPath(["a", "f", "i"]);
        g.SetEdge("a", "e");
        await Assert.That(CrossCount.Run(g, [["a", "d"], ["b", "e", "f"], ["c", "i"]])).IsEqualTo(1);
        await Assert.That(CrossCount.Run(g, [["d", "a"], ["e", "b", "f"], ["c", "i"]])).IsEqualTo(0);
    }
}
