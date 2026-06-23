public class AddSubgraphConstraintsTests
{
    Graph graph = null!;
    Graph constraintGraph = null!;

    [Before(Test)]
    public void Setup()
    {
        graph = new(compound: true);
        constraintGraph = new();
    }

    [Test]
    public async Task DoesNotChangeCgForAFlatSetOfNodes()
    {
        var vs = new List<string> { "a", "b", "c", "d" };
        foreach (var v in vs)
        {
            graph.SetNode(v);
        }

        AddSubgraphConstraints.Run(graph, constraintGraph, vs);
        await Assert.That(constraintGraph.NodeCount).IsEqualTo(0);
        await Assert.That(constraintGraph.Edges().Count).IsEqualTo(0);
    }

    [Test]
    public async Task DoesNotCreateAConstraintForContiguousSubgraphNodes()
    {
        var vs = new List<string> { "a", "b", "c" };
        foreach (var v in vs)
        {
            graph.SetParent(v, "sg");
        }

        AddSubgraphConstraints.Run(graph, constraintGraph, vs);
        await Assert.That(constraintGraph.NodeCount).IsEqualTo(0);
        await Assert.That(constraintGraph.Edges().Count).IsEqualTo(0);
    }

    [Test]
    public async Task AddsAConstraintWhenTheParentsForAdjacentNodesAreDifferent()
    {
        var vs = new List<string> { "a", "b" };
        graph.SetParent("a", "sg1");
        graph.SetParent("b", "sg2");
        AddSubgraphConstraints.Run(graph, constraintGraph, vs);
        await Assert.That(constraintGraph.Edges()).IsEquivalentTo(new List<Edge> { new("sg1", "sg2") });
    }

    [Test]
    public async Task WorksForMultipleLevels()
    {
        var vs = new List<string> { "a", "b", "c", "d", "e", "f", "graph", "h" };
        foreach (var v in vs)
        {
            graph.SetNode(v);
        }

        graph.SetParent("b", "sg2");
        graph.SetParent("sg2", "sg1");
        graph.SetParent("c", "sg1");
        graph.SetParent("d", "sg3");
        graph.SetParent("sg3", "sg1");
        graph.SetParent("f", "sg4");
        graph.SetParent("graph", "sg5");
        graph.SetParent("sg5", "sg4");
        AddSubgraphConstraints.Run(graph, constraintGraph, vs);

        var edges = constraintGraph.Edges()
            .OrderBy(e => e.V, StringComparer.Ordinal)
            .ToList();
        await Assert.That(edges).IsEquivalentTo(new List<Edge>
        {
            new("sg1", "sg4"),
            new("sg2", "sg3")
        });
    }
}
