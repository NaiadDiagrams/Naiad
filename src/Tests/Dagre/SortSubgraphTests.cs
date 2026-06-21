using TUnit.Assertions.Enums;

namespace Naiad.Dagre.Tests;

public class SortSubgraphTests
{
    Graph g = null!;
    Graph constraintGraph = null!;

    [Before(Test)]
    public void Setup()
    {
        g = new Graph(compound: true)
            .SetDefaultNodeLabel(_ => new NodeLabel())
            .SetDefaultEdgeLabel((_, _, _) => new EdgeLabel { Weight = 1 });
        var ids = new[] { "0", "1", "2", "3", "4" };
        for (var i = 0; i < ids.Length; i++)
        {
            g.SetNode(ids[i], new NodeLabel { Order = i });
        }

        constraintGraph = new Graph();
    }

    [Test]
    public async Task SortsAFlatSubgraphBasedOnBarycenter()
    {
        g.SetEdge("3", "x");
        g.SetEdge("1", "y", new EdgeLabel { Weight = 2 });
        g.SetEdge("4", "y");
        foreach (var v in new[] { "x", "y" })
        {
            g.SetParent(v, "movable");
        }

        await Assert.That(SortSubgraph.Run(g, "movable", constraintGraph).Vs)
            .IsEquivalentTo(new List<string> { "y", "x" }, CollectionOrdering.Matching);
    }

    [Test]
    public async Task PreservesThePosOfANodeWithoutNeighborsInAFlatSubgraph()
    {
        g.SetEdge("3", "x");
        g.SetNode("y");
        g.SetEdge("1", "z", new EdgeLabel { Weight = 2 });
        g.SetEdge("4", "z");
        foreach (var v in new[] { "x", "y", "z" })
        {
            g.SetParent(v, "movable");
        }

        await Assert.That(SortSubgraph.Run(g, "movable", constraintGraph).Vs)
            .IsEquivalentTo(new List<string> { "z", "y", "x" }, CollectionOrdering.Matching);
    }

    [Test]
    public async Task BiasesToTheLeftWithoutReverseBias()
    {
        g.SetEdge("1", "x");
        g.SetEdge("1", "y");
        foreach (var v in new[] { "x", "y" })
        {
            g.SetParent(v, "movable");
        }

        await Assert.That(SortSubgraph.Run(g, "movable", constraintGraph).Vs)
            .IsEquivalentTo(new List<string> { "x", "y" }, CollectionOrdering.Matching);
    }

    [Test]
    public async Task BiasesToTheRightWithReverseBias()
    {
        g.SetEdge("1", "x");
        g.SetEdge("1", "y");
        foreach (var v in new[] { "x", "y" })
        {
            g.SetParent(v, "movable");
        }

        await Assert.That(SortSubgraph.Run(g, "movable", constraintGraph, true).Vs)
            .IsEquivalentTo(new List<string> { "y", "x" }, CollectionOrdering.Matching);
    }

    [Test]
    public async Task AggregatesStatsAboutTheSubgraph()
    {
        g.SetEdge("3", "x");
        g.SetEdge("1", "y", new EdgeLabel { Weight = 2 });
        g.SetEdge("4", "y");
        foreach (var v in new[] { "x", "y" })
        {
            g.SetParent(v, "movable");
        }

        var results = SortSubgraph.Run(g, "movable", constraintGraph);
        await Assert.That(results.Barycenter).IsEqualTo(2.25);
        await Assert.That(results.Weight).IsEqualTo(4);
    }

    [Test]
    public async Task CanSortANestedSubgraphWithNoBarycenter()
    {
        g.SetNodes(["a", "b", "c"]);
        g.SetParent("a", "y");
        g.SetParent("b", "y");
        g.SetParent("c", "y");
        g.SetEdge("0", "x");
        g.SetEdge("1", "z");
        g.SetEdge("2", "y");
        foreach (var v in new[] { "x", "y", "z" })
        {
            g.SetParent(v, "movable");
        }

        await Assert.That(SortSubgraph.Run(g, "movable", constraintGraph).Vs)
            .IsEquivalentTo(new List<string> { "x", "z", "a", "b", "c" }, CollectionOrdering.Matching);
    }

    [Test]
    public async Task CanSortANestedSubgraphWithABarycenter()
    {
        g.SetNodes(["a", "b", "c"]);
        g.SetParent("a", "y");
        g.SetParent("b", "y");
        g.SetParent("c", "y");
        g.SetEdge("0", "a", new EdgeLabel { Weight = 3 });
        g.SetEdge("0", "x");
        g.SetEdge("1", "z");
        g.SetEdge("2", "y");
        foreach (var v in new[] { "x", "y", "z" })
        {
            g.SetParent(v, "movable");
        }

        await Assert.That(SortSubgraph.Run(g, "movable", constraintGraph).Vs)
            .IsEquivalentTo(new List<string> { "x", "a", "b", "c", "z" }, CollectionOrdering.Matching);
    }

    [Test]
    public async Task CanSortANestedSubgraphWithNoInEdges()
    {
        g.SetNodes(["a", "b", "c"]);
        g.SetParent("a", "y");
        g.SetParent("b", "y");
        g.SetParent("c", "y");
        g.SetEdge("0", "a");
        g.SetEdge("1", "b");
        g.SetEdge("0", "x");
        g.SetEdge("1", "z");
        foreach (var v in new[] { "x", "y", "z" })
        {
            g.SetParent(v, "movable");
        }

        await Assert.That(SortSubgraph.Run(g, "movable", constraintGraph).Vs)
            .IsEquivalentTo(new List<string> { "x", "a", "b", "c", "z" }, CollectionOrdering.Matching);
    }

    [Test]
    public async Task SortsBorderNodesToTheExtremesOfTheSubgraph()
    {
        g.SetEdge("0", "x");
        g.SetEdge("1", "y");
        g.SetEdge("2", "z");
        g.SetNode("sg1", new NodeLabel { BorderLeftId = "bl", BorderRightId = "br" });
        foreach (var v in new[] { "x", "y", "z", "bl", "br" })
        {
            g.SetParent(v, "sg1");
        }

        await Assert.That(SortSubgraph.Run(g, "sg1", constraintGraph).Vs)
            .IsEquivalentTo(new List<string> { "bl", "x", "y", "z", "br" }, CollectionOrdering.Matching);
    }

    [Test]
    public async Task AssignsABarycenterToASubgraphBasedOnPreviousBorderNodes()
    {
        g.SetNode("bl1", new NodeLabel { Order = 0 });
        g.SetNode("br1", new NodeLabel { Order = 1 });
        g.SetEdge("bl1", "bl2");
        g.SetEdge("br1", "br2");
        foreach (var v in new[] { "bl2", "br2" })
        {
            g.SetParent(v, "sg");
        }

        g.SetNode("sg", new NodeLabel { BorderLeftId = "bl2", BorderRightId = "br2" });

        var result = SortSubgraph.Run(g, "sg", constraintGraph);
        await Assert.That(result.Barycenter).IsEqualTo(0.5);
        await Assert.That(result.Weight).IsEqualTo(2);
        await Assert.That(result.Vs).IsEquivalentTo(new List<string> { "bl2", "br2" }, CollectionOrdering.Matching);
    }
}
