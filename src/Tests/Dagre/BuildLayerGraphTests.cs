using TUnit.Assertions.Enums;

namespace Naiad.Dagre.Tests;

public class BuildLayerGraphTests
{
    Graph g = null!;

    [Before(Test)]
    public void Setup() => g = new(compound: true, multigraph: true);

    [Test]
    public async Task PlacesMovableNodesWithNoParentsUnderTheRootNode()
    {
        g.SetNode("a", new() { Rank = 1 });
        g.SetNode("b", new() { Rank = 1 });
        g.SetNode("c", new() { Rank = 2 });
        g.SetNode("d", new() { Rank = 3 });

        var lg = BuildLayerGraph.Run(g, 1, "inEdges");
        await Assert.That(lg.HasNode(lg.Graph_().Root!)).IsTrue();
        await Assert.That(lg.Children(Graph.GraphNode)).IsEquivalentTo(new List<string> { lg.Graph_().Root! }, CollectionOrdering.Matching);
        await Assert.That(lg.Children(lg.Graph_().Root!)).IsEquivalentTo(new List<string> { "a", "b" }, CollectionOrdering.Matching);
    }

    [Test]
    public async Task CopiesFlatNodesFromTheLayerToTheGraph()
    {
        g.SetNode("a", new() { Rank = 1 });
        g.SetNode("b", new() { Rank = 1 });
        g.SetNode("c", new() { Rank = 2 });
        g.SetNode("d", new() { Rank = 3 });

        await Assert.That(BuildLayerGraph.Run(g, 1, "inEdges").Nodes()).Contains("a");
        await Assert.That(BuildLayerGraph.Run(g, 1, "inEdges").Nodes()).Contains("b");
        await Assert.That(BuildLayerGraph.Run(g, 2, "inEdges").Nodes()).Contains("c");
        await Assert.That(BuildLayerGraph.Run(g, 3, "inEdges").Nodes()).Contains("d");
    }

    [Test]
    public async Task UsesTheOriginalNodeLabelForCopiedNodes()
    {
        // This allows us to make updates to the original graph and have them
        // be available automatically in the layer graph.
        // The TS test uses a dynamic `foo` field; here we verify the same
        // reference-sharing semantics through the strongly-typed `Order` field.
        g.SetNode("a", new() { Order = 1, Rank = 1 });
        g.SetNode("b", new() { Order = 2, Rank = 2 });
        g.SetEdge("a", "b", new() { Weight = 1 });

        var lg = BuildLayerGraph.Run(g, 2, "inEdges");

        await Assert.That(lg.Node("a").Order).IsEqualTo(1);
        g.Node("a").Order = 99;
        await Assert.That(lg.Node("a").Order).IsEqualTo(99);

        await Assert.That(lg.Node("b").Order).IsEqualTo(2);
        g.Node("b").Order = 99;
        await Assert.That(lg.Node("b").Order).IsEqualTo(99);
    }

    [Test]
    public async Task CopiesEdgesIncidentOnRankNodesToTheGraphInEdges()
    {
        g.SetNode("a", new() { Rank = 1 });
        g.SetNode("b", new() { Rank = 1 });
        g.SetNode("c", new() { Rank = 2 });
        g.SetNode("d", new() { Rank = 3 });
        g.SetEdge("a", "c", new() { Weight = 2 });
        g.SetEdge("b", "c", new() { Weight = 3 });
        g.SetEdge("c", "d", new() { Weight = 4 });

        await Assert.That(BuildLayerGraph.Run(g, 1, "inEdges").EdgeCount).IsEqualTo(0);
        await Assert.That(BuildLayerGraph.Run(g, 2, "inEdges").EdgeCount).IsEqualTo(2);
        await Assert.That(BuildLayerGraph.Run(g, 2, "inEdges").Edge_("a", "c").Weight).IsEqualTo(2);
        await Assert.That(BuildLayerGraph.Run(g, 2, "inEdges").Edge_("b", "c").Weight).IsEqualTo(3);
        await Assert.That(BuildLayerGraph.Run(g, 3, "inEdges").EdgeCount).IsEqualTo(1);
        await Assert.That(BuildLayerGraph.Run(g, 3, "inEdges").Edge_("c", "d").Weight).IsEqualTo(4);
    }

    [Test]
    public async Task CopiesEdgesIncidentOnRankNodesToTheGraphOutEdges()
    {
        g.SetNode("a", new() { Rank = 1 });
        g.SetNode("b", new() { Rank = 1 });
        g.SetNode("c", new() { Rank = 2 });
        g.SetNode("d", new() { Rank = 3 });
        g.SetEdge("a", "c", new() { Weight = 2 });
        g.SetEdge("b", "c", new() { Weight = 3 });
        g.SetEdge("c", "d", new() { Weight = 4 });

        await Assert.That(BuildLayerGraph.Run(g, 1, "outEdges").EdgeCount).IsEqualTo(2);
        await Assert.That(BuildLayerGraph.Run(g, 1, "outEdges").Edge_("c", "a").Weight).IsEqualTo(2);
        await Assert.That(BuildLayerGraph.Run(g, 1, "outEdges").Edge_("c", "b").Weight).IsEqualTo(3);
        await Assert.That(BuildLayerGraph.Run(g, 2, "outEdges").EdgeCount).IsEqualTo(1);
        await Assert.That(BuildLayerGraph.Run(g, 2, "outEdges").Edge_("d", "c").Weight).IsEqualTo(4);
        await Assert.That(BuildLayerGraph.Run(g, 3, "outEdges").EdgeCount).IsEqualTo(0);
    }

    [Test]
    public async Task CollapsesMultiEdges()
    {
        g.SetNode("a", new() { Rank = 1 });
        g.SetNode("b", new() { Rank = 2 });
        g.SetEdge("a", "b", new() { Weight = 2 });
        g.SetEdge("a", "b", new() { Weight = 3 }, "multi");

        await Assert.That(BuildLayerGraph.Run(g, 2, "inEdges").Edge_("a", "b").Weight).IsEqualTo(5);
    }

    [Test]
    public async Task PreservesHierarchyForTheMovableLayer()
    {
        g.SetNode("a", new() { Rank = 0 });
        g.SetNode("b", new() { Rank = 0 });
        g.SetNode("c", new() { Rank = 0 });
        g.SetNode("sg", new()
        {
            MinRank = 0,
            MaxRank = 0,
            BorderLeft = ["bl"],
            BorderRight = ["br"]
        });
        foreach (var v in new[] { "a", "b" })
        {
            g.SetParent(v, "sg");
        }

        var lg = BuildLayerGraph.Run(g, 0, "inEdges");
        var root = lg.Graph_().Root!;
        var children = lg.Children(root);
        children.Sort(StringComparer.Ordinal);
        await Assert.That(children).IsEquivalentTo(new List<string> { "c", "sg" }, CollectionOrdering.Matching);
        await Assert.That(lg.Parent("a")).IsEqualTo("sg");
        await Assert.That(lg.Parent("b")).IsEqualTo("sg");
    }
}
