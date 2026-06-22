using TUnit.Assertions.Enums;

namespace Naiad.Dagre.Tests;

public class SortSubgraphTests
{
    Graph graph = null!;
    Graph constraintGraph = null!;

    [Before(Test)]
    public void Setup()
    {
        graph = new Graph(compound: true)
            .SetDefaultNodeLabel(_ => new())
            .SetDefaultEdgeLabel((_, _, _) => new() { Weight = 1 });
        var ids = new[] { "0", "1", "2", "3", "4" };
        for (var i = 0; i < ids.Length; i++)
        {
            graph.SetNode(ids[i], new() { Order = i });
        }

        constraintGraph = new();
    }

    [Test]
    public async Task SortsAFlatSubgraphBasedOnBarycenter()
    {
        graph.SetEdge("3", "x");
        graph.SetEdge("1", "y", new() { Weight = 2 });
        graph.SetEdge("4", "y");
        foreach (var v in new[] { "x", "y" })
        {
            graph.SetParent(v, "movable");
        }

        await Assert.That(SortSubgraph.Run(graph, "movable", constraintGraph).Vs)
            .IsEquivalentTo(new List<string> { "y", "x" }, CollectionOrdering.Matching);
    }

    [Test]
    public async Task PreservesThePosOfANodeWithoutNeighborsInAFlatSubgraph()
    {
        graph.SetEdge("3", "x");
        graph.SetNode("y");
        graph.SetEdge("1", "z", new() { Weight = 2 });
        graph.SetEdge("4", "z");
        foreach (var v in new[] { "x", "y", "z" })
        {
            graph.SetParent(v, "movable");
        }

        await Assert.That(SortSubgraph.Run(graph, "movable", constraintGraph).Vs)
            .IsEquivalentTo(new List<string> { "z", "y", "x" }, CollectionOrdering.Matching);
    }

    [Test]
    public async Task BiasesToTheLeftWithoutReverseBias()
    {
        graph.SetEdge("1", "x");
        graph.SetEdge("1", "y");
        foreach (var v in new[] { "x", "y" })
        {
            graph.SetParent(v, "movable");
        }

        await Assert.That(SortSubgraph.Run(graph, "movable", constraintGraph).Vs)
            .IsEquivalentTo(new List<string> { "x", "y" }, CollectionOrdering.Matching);
    }

    [Test]
    public async Task BiasesToTheRightWithReverseBias()
    {
        graph.SetEdge("1", "x");
        graph.SetEdge("1", "y");
        foreach (var v in new[] { "x", "y" })
        {
            graph.SetParent(v, "movable");
        }

        await Assert.That(SortSubgraph.Run(graph, "movable", constraintGraph, true).Vs)
            .IsEquivalentTo(new List<string> { "y", "x" }, CollectionOrdering.Matching);
    }

    [Test]
    public async Task AggregatesStatsAboutTheSubgraph()
    {
        graph.SetEdge("3", "x");
        graph.SetEdge("1", "y", new() { Weight = 2 });
        graph.SetEdge("4", "y");
        foreach (var v in new[] { "x", "y" })
        {
            graph.SetParent(v, "movable");
        }

        var results = SortSubgraph.Run(graph, "movable", constraintGraph);
        await Assert.That(results.Barycenter).IsEqualTo(2.25);
        await Assert.That(results.Weight).IsEqualTo(4);
    }

    [Test]
    public async Task CanSortANestedSubgraphWithNoBarycenter()
    {
        graph.SetNodes(["a", "b", "c"]);
        graph.SetParent("a", "y");
        graph.SetParent("b", "y");
        graph.SetParent("c", "y");
        graph.SetEdge("0", "x");
        graph.SetEdge("1", "z");
        graph.SetEdge("2", "y");
        foreach (var v in new[] { "x", "y", "z" })
        {
            graph.SetParent(v, "movable");
        }

        await Assert.That(SortSubgraph.Run(graph, "movable", constraintGraph).Vs)
            .IsEquivalentTo(new List<string> { "x", "z", "a", "b", "c" }, CollectionOrdering.Matching);
    }

    [Test]
    public async Task CanSortANestedSubgraphWithABarycenter()
    {
        graph.SetNodes(["a", "b", "c"]);
        graph.SetParent("a", "y");
        graph.SetParent("b", "y");
        graph.SetParent("c", "y");
        graph.SetEdge("0", "a", new() { Weight = 3 });
        graph.SetEdge("0", "x");
        graph.SetEdge("1", "z");
        graph.SetEdge("2", "y");
        foreach (var v in new[] { "x", "y", "z" })
        {
            graph.SetParent(v, "movable");
        }

        await Assert.That(SortSubgraph.Run(graph, "movable", constraintGraph).Vs)
            .IsEquivalentTo(new List<string> { "x", "a", "b", "c", "z" }, CollectionOrdering.Matching);
    }

    [Test]
    public async Task CanSortANestedSubgraphWithNoInEdges()
    {
        graph.SetNodes(["a", "b", "c"]);
        graph.SetParent("a", "y");
        graph.SetParent("b", "y");
        graph.SetParent("c", "y");
        graph.SetEdge("0", "a");
        graph.SetEdge("1", "b");
        graph.SetEdge("0", "x");
        graph.SetEdge("1", "z");
        foreach (var v in new[] { "x", "y", "z" })
        {
            graph.SetParent(v, "movable");
        }

        await Assert.That(SortSubgraph.Run(graph, "movable", constraintGraph).Vs)
            .IsEquivalentTo(new List<string> { "x", "a", "b", "c", "z" }, CollectionOrdering.Matching);
    }

    [Test]
    public async Task SortsBorderNodesToTheExtremesOfTheSubgraph()
    {
        graph.SetEdge("0", "x");
        graph.SetEdge("1", "y");
        graph.SetEdge("2", "z");
        graph.SetNode("sg1", new() { BorderLeftId = "bl", BorderRightId = "br" });
        foreach (var v in new[] { "x", "y", "z", "bl", "br" })
        {
            graph.SetParent(v, "sg1");
        }

        await Assert.That(SortSubgraph.Run(graph, "sg1", constraintGraph).Vs)
            .IsEquivalentTo(new List<string> { "bl", "x", "y", "z", "br" }, CollectionOrdering.Matching);
    }

    [Test]
    public async Task AssignsABarycenterToASubgraphBasedOnPreviousBorderNodes()
    {
        graph.SetNode("bl1", new() { Order = 0 });
        graph.SetNode("br1", new() { Order = 1 });
        graph.SetEdge("bl1", "bl2");
        graph.SetEdge("br1", "br2");
        foreach (var v in new[] { "bl2", "br2" })
        {
            graph.SetParent(v, "sg");
        }

        graph.SetNode("sg", new() { BorderLeftId = "bl2", BorderRightId = "br2" });

        var result = SortSubgraph.Run(graph, "sg", constraintGraph);
        await Assert.That(result.Barycenter).IsEqualTo(0.5);
        await Assert.That(result.Weight).IsEqualTo(2);
        await Assert.That(result.Vs).IsEquivalentTo(new List<string> { "bl2", "br2" }, CollectionOrdering.Matching);
    }
}
