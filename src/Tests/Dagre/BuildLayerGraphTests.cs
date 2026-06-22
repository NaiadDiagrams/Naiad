using TUnit.Assertions.Enums;

namespace Naiad.Dagre.Tests;

public class BuildLayerGraphTests
{
    Graph graph = null!;

    [Before(Test)]
    public void Setup() => graph = new(compound: true, multigraph: true);

    [Test]
    public async Task PlacesMovableNodesWithNoParentsUnderTheRootNode()
    {
        graph.SetNode("a", new() { Rank = 1 });
        graph.SetNode("b", new() { Rank = 1 });
        graph.SetNode("c", new() { Rank = 2 });
        graph.SetNode("d", new() { Rank = 3 });

        var lg = BuildLayerGraph.Run(graph, 1, "inEdges");
        await Assert.That(lg.HasNode(lg.Label.Root!)).IsTrue();
        await Assert.That(lg.Children(Graph.GraphNode)).IsEquivalentTo(new List<string> { lg.Label.Root! }, CollectionOrdering.Matching);
        await Assert.That(lg.Children(lg.Label.Root!)).IsEquivalentTo(new List<string> { "a", "b" }, CollectionOrdering.Matching);
    }

    [Test]
    public async Task CopiesFlatNodesFromTheLayerToTheGraph()
    {
        graph.SetNode("a", new() { Rank = 1 });
        graph.SetNode("b", new() { Rank = 1 });
        graph.SetNode("c", new() { Rank = 2 });
        graph.SetNode("d", new() { Rank = 3 });

        await Assert.That(BuildLayerGraph.Run(graph, 1, "inEdges").Nodes()).Contains("a");
        await Assert.That(BuildLayerGraph.Run(graph, 1, "inEdges").Nodes()).Contains("b");
        await Assert.That(BuildLayerGraph.Run(graph, 2, "inEdges").Nodes()).Contains("c");
        await Assert.That(BuildLayerGraph.Run(graph, 3, "inEdges").Nodes()).Contains("d");
    }

    [Test]
    public async Task UsesTheOriginalNodeLabelForCopiedNodes()
    {
        // This allows us to make updates to the original graph and have them
        // be available automatically in the layer graph.
        // The TS test uses a dynamic `foo` field; here we verify the same
        // reference-sharing semantics through the strongly-typed `Order` field.
        graph.SetNode("a", new() { Order = 1, Rank = 1 });
        graph.SetNode("b", new() { Order = 2, Rank = 2 });
        graph.SetEdge("a", "b", new() { Weight = 1 });

        var lg = BuildLayerGraph.Run(graph, 2, "inEdges");

        await Assert.That(lg.NodeLabel("a").Order).IsEqualTo(1);
        graph.NodeLabel("a").Order = 99;
        await Assert.That(lg.NodeLabel("a").Order).IsEqualTo(99);

        await Assert.That(lg.NodeLabel("b").Order).IsEqualTo(2);
        graph.NodeLabel("b").Order = 99;
        await Assert.That(lg.NodeLabel("b").Order).IsEqualTo(99);
    }

    [Test]
    public async Task CopiesEdgesIncidentOnRankNodesToTheGraphInEdges()
    {
        graph.SetNode("a", new() { Rank = 1 });
        graph.SetNode("b", new() { Rank = 1 });
        graph.SetNode("c", new() { Rank = 2 });
        graph.SetNode("d", new() { Rank = 3 });
        graph.SetEdge("a", "c", new() { Weight = 2 });
        graph.SetEdge("b", "c", new() { Weight = 3 });
        graph.SetEdge("c", "d", new() { Weight = 4 });

        await Assert.That(BuildLayerGraph.Run(graph, 1, "inEdges").EdgeCount).IsEqualTo(0);
        await Assert.That(BuildLayerGraph.Run(graph, 2, "inEdges").EdgeCount).IsEqualTo(2);
        await Assert.That(BuildLayerGraph.Run(graph, 2, "inEdges").FindEdgeLabel("a", "c").Weight).IsEqualTo(2);
        await Assert.That(BuildLayerGraph.Run(graph, 2, "inEdges").FindEdgeLabel("b", "c").Weight).IsEqualTo(3);
        await Assert.That(BuildLayerGraph.Run(graph, 3, "inEdges").EdgeCount).IsEqualTo(1);
        await Assert.That(BuildLayerGraph.Run(graph, 3, "inEdges").FindEdgeLabel("c", "d").Weight).IsEqualTo(4);
    }

    [Test]
    public async Task CopiesEdgesIncidentOnRankNodesToTheGraphOutEdges()
    {
        graph.SetNode("a", new() { Rank = 1 });
        graph.SetNode("b", new() { Rank = 1 });
        graph.SetNode("c", new() { Rank = 2 });
        graph.SetNode("d", new() { Rank = 3 });
        graph.SetEdge("a", "c", new() { Weight = 2 });
        graph.SetEdge("b", "c", new() { Weight = 3 });
        graph.SetEdge("c", "d", new() { Weight = 4 });

        await Assert.That(BuildLayerGraph.Run(graph, 1, "outEdges").EdgeCount).IsEqualTo(2);
        await Assert.That(BuildLayerGraph.Run(graph, 1, "outEdges").FindEdgeLabel("c", "a").Weight).IsEqualTo(2);
        await Assert.That(BuildLayerGraph.Run(graph, 1, "outEdges").FindEdgeLabel("c", "b").Weight).IsEqualTo(3);
        await Assert.That(BuildLayerGraph.Run(graph, 2, "outEdges").EdgeCount).IsEqualTo(1);
        await Assert.That(BuildLayerGraph.Run(graph, 2, "outEdges").FindEdgeLabel("d", "c").Weight).IsEqualTo(4);
        await Assert.That(BuildLayerGraph.Run(graph, 3, "outEdges").EdgeCount).IsEqualTo(0);
    }

    [Test]
    public async Task CollapsesMultiEdges()
    {
        graph.SetNode("a", new() { Rank = 1 });
        graph.SetNode("b", new() { Rank = 2 });
        graph.SetEdge("a", "b", new() { Weight = 2 });
        graph.SetEdge("a", "b", new() { Weight = 3 }, "multi");

        await Assert.That(BuildLayerGraph.Run(graph, 2, "inEdges").FindEdgeLabel("a", "b").Weight).IsEqualTo(5);
    }

    [Test]
    public async Task PreservesHierarchyForTheMovableLayer()
    {
        graph.SetNode("a", new() { Rank = 0 });
        graph.SetNode("b", new() { Rank = 0 });
        graph.SetNode("c", new() { Rank = 0 });
        graph.SetNode("sg", new()
        {
            MinRank = 0,
            MaxRank = 0,
            BorderLeft = ["bl"],
            BorderRight = ["br"]
        });
        foreach (var v in new[] { "a", "b" })
        {
            graph.SetParent(v, "sg");
        }

        var lg = BuildLayerGraph.Run(graph, 0, "inEdges");
        var root = lg.Label.Root!;
        var children = lg.Children(root);
        children.Sort(StringComparer.Ordinal);
        await Assert.That(children).IsEquivalentTo(new List<string> { "c", "sg" }, CollectionOrdering.Matching);
        await Assert.That(lg.Parent("a")).IsEqualTo("sg");
        await Assert.That(lg.Parent("b")).IsEqualTo("sg");
    }
}
