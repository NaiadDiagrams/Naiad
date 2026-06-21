namespace Naiad.Dagre.Tests;

public class ResolveConflictsTests
{
    Graph constraintGraph = null!;

    [Before(Test)]
    public void Setup() => constraintGraph = new Graph();

    static List<ResolvedEntry> SortByFirstV(List<ResolvedEntry> entries) =>
        entries.OrderBy(e => e.Vs[0], StringComparer.Ordinal).ToList();

    static string Join(List<string> vs) => string.Join(",", vs);

    [Test]
    public async Task ReturnsBackNodesUnchangedWhenNoConstraintsExist()
    {
        var input = new List<BarycenterEntry>
        {
            new() { V = "a", Barycenter = 2, Weight = 3 },
            new() { V = "b", Barycenter = 1, Weight = 2 }
        };

        var results = SortByFirstV(ResolveConflicts.Run(input, constraintGraph));
        await Assert.That(results.Count).IsEqualTo(2);

        await Assert.That(results[0].Vs).IsEquivalentTo(new List<string> { "a" });
        await Assert.That(results[0].I).IsEqualTo(0);
        await Assert.That(results[0].Barycenter!.Value).IsEqualTo(2).Within(0.001);
        await Assert.That(results[0].Weight!.Value).IsEqualTo(3).Within(0.001);

        await Assert.That(results[1].Vs).IsEquivalentTo(new List<string> { "b" });
        await Assert.That(results[1].I).IsEqualTo(1);
        await Assert.That(results[1].Barycenter!.Value).IsEqualTo(1).Within(0.001);
        await Assert.That(results[1].Weight!.Value).IsEqualTo(2).Within(0.001);
    }

    [Test]
    public async Task ReturnsBackNodesUnchangedWhenNoConflictsExist()
    {
        var input = new List<BarycenterEntry>
        {
            new() { V = "a", Barycenter = 2, Weight = 3 },
            new() { V = "b", Barycenter = 1, Weight = 2 }
        };
        constraintGraph.SetEdge("b", "a");

        var results = SortByFirstV(ResolveConflicts.Run(input, constraintGraph));
        await Assert.That(results.Count).IsEqualTo(2);

        await Assert.That(results[0].Vs).IsEquivalentTo(new List<string> { "a" });
        await Assert.That(results[0].I).IsEqualTo(0);
        await Assert.That(results[0].Barycenter!.Value).IsEqualTo(2).Within(0.001);
        await Assert.That(results[0].Weight!.Value).IsEqualTo(3).Within(0.001);

        await Assert.That(results[1].Vs).IsEquivalentTo(new List<string> { "b" });
        await Assert.That(results[1].I).IsEqualTo(1);
        await Assert.That(results[1].Barycenter!.Value).IsEqualTo(1).Within(0.001);
        await Assert.That(results[1].Weight!.Value).IsEqualTo(2).Within(0.001);
    }

    [Test]
    public async Task CoalescesNodesWhenThereIsAConflict()
    {
        var input = new List<BarycenterEntry>
        {
            new() { V = "a", Barycenter = 2, Weight = 3 },
            new() { V = "b", Barycenter = 1, Weight = 2 }
        };
        constraintGraph.SetEdge("a", "b");

        var results = ResolveConflicts.Run(input, constraintGraph);
        await Assert.That(results.Count).IsEqualTo(1);
        await Assert.That(Join(results[0].Vs)).IsEqualTo("a,b");
        await Assert.That(results[0].I).IsEqualTo(0);
        await Assert.That(results[0].Barycenter!.Value).IsEqualTo((3 * 2 + 2 * 1) / (3.0 + 2)).Within(0.001);
        await Assert.That(results[0].Weight!.Value).IsEqualTo(3 + 2).Within(0.001);
    }

    [Test]
    public async Task CoalescesNodesWhenThereIsAConflict2()
    {
        var input = new List<BarycenterEntry>
        {
            new() { V = "a", Barycenter = 4, Weight = 1 },
            new() { V = "b", Barycenter = 3, Weight = 1 },
            new() { V = "c", Barycenter = 2, Weight = 1 },
            new() { V = "d", Barycenter = 1, Weight = 1 }
        };
        constraintGraph.SetPath(["a", "b", "c", "d"]);

        var results = ResolveConflicts.Run(input, constraintGraph);
        await Assert.That(results.Count).IsEqualTo(1);
        await Assert.That(Join(results[0].Vs)).IsEqualTo("a,b,c,d");
        await Assert.That(results[0].I).IsEqualTo(0);
        await Assert.That(results[0].Barycenter!.Value).IsEqualTo((4 + 3 + 2 + 1) / 4.0).Within(0.001);
        await Assert.That(results[0].Weight!.Value).IsEqualTo(4).Within(0.001);
    }

    [Test]
    public async Task WorksWithMultipleConstraintsForTheSameTarget1()
    {
        var input = new List<BarycenterEntry>
        {
            new() { V = "a", Barycenter = 4, Weight = 1 },
            new() { V = "b", Barycenter = 3, Weight = 1 },
            new() { V = "c", Barycenter = 2, Weight = 1 }
        };
        constraintGraph.SetEdge("a", "c");
        constraintGraph.SetEdge("b", "c");

        var results = ResolveConflicts.Run(input, constraintGraph);
        await Assert.That(results.Count).IsEqualTo(1);
        var result = results[0];
        await Assert.That(result.Vs.IndexOf("c")).IsGreaterThan(result.Vs.IndexOf("a"));
        await Assert.That(result.Vs.IndexOf("c")).IsGreaterThan(result.Vs.IndexOf("b"));
        await Assert.That(result.I).IsEqualTo(0);
        await Assert.That(result.Barycenter!.Value).IsEqualTo((4 + 3 + 2) / 3.0).Within(0.001);
        await Assert.That(result.Weight!.Value).IsEqualTo(3).Within(0.001);
    }

    [Test]
    public async Task WorksWithMultipleConstraintsForTheSameTarget2()
    {
        var input = new List<BarycenterEntry>
        {
            new() { V = "a", Barycenter = 4, Weight = 1 },
            new() { V = "b", Barycenter = 3, Weight = 1 },
            new() { V = "c", Barycenter = 2, Weight = 1 },
            new() { V = "d", Barycenter = 1, Weight = 1 }
        };
        constraintGraph.SetEdge("a", "c");
        constraintGraph.SetEdge("a", "d");
        constraintGraph.SetEdge("b", "c");
        constraintGraph.SetEdge("c", "d");

        var results = ResolveConflicts.Run(input, constraintGraph);
        await Assert.That(results.Count).IsEqualTo(1);
        var result = results[0];
        await Assert.That(result.Vs.IndexOf("c")).IsGreaterThan(result.Vs.IndexOf("a"));
        await Assert.That(result.Vs.IndexOf("c")).IsGreaterThan(result.Vs.IndexOf("b"));
        await Assert.That(result.Vs.IndexOf("d")).IsGreaterThan(result.Vs.IndexOf("c"));
        await Assert.That(result.I).IsEqualTo(0);
        await Assert.That(result.Barycenter!.Value).IsEqualTo((4 + 3 + 2 + 1) / 4.0).Within(0.001);
        await Assert.That(result.Weight!.Value).IsEqualTo(4).Within(0.001);
    }

    [Test]
    public async Task DoesNothingToANodeLackingBothABarycenterAndAConstraint()
    {
        var input = new List<BarycenterEntry>
        {
            new() { V = "a" },
            new() { V = "b", Barycenter = 1, Weight = 2 }
        };

        var results = SortByFirstV(ResolveConflicts.Run(input, constraintGraph));
        await Assert.That(results.Count).IsEqualTo(2);

        await Assert.That(results[0].Vs).IsEquivalentTo(new List<string> { "a" });
        await Assert.That(results[0].I).IsEqualTo(0);
        await Assert.That(results[0].Barycenter).IsNull();
        await Assert.That(results[0].Weight).IsNull();

        await Assert.That(results[1].Vs).IsEquivalentTo(new List<string> { "b" });
        await Assert.That(results[1].I).IsEqualTo(1);
        await Assert.That(results[1].Barycenter!.Value).IsEqualTo(1).Within(0.001);
        await Assert.That(results[1].Weight!.Value).IsEqualTo(2).Within(0.001);
    }

    [Test]
    public async Task TreatsANodeWithoutABarycenterAsAlwaysViolatingConstraints1()
    {
        var input = new List<BarycenterEntry>
        {
            new() { V = "a" },
            new() { V = "b", Barycenter = 1, Weight = 2 }
        };
        constraintGraph.SetEdge("a", "b");

        var results = ResolveConflicts.Run(input, constraintGraph);
        await Assert.That(results.Count).IsEqualTo(1);
        await Assert.That(Join(results[0].Vs)).IsEqualTo("a,b");
        await Assert.That(results[0].I).IsEqualTo(0);
        await Assert.That(results[0].Barycenter!.Value).IsEqualTo(1).Within(0.001);
        await Assert.That(results[0].Weight!.Value).IsEqualTo(2).Within(0.001);
    }

    [Test]
    public async Task TreatsANodeWithoutABarycenterAsAlwaysViolatingConstraints2()
    {
        var input = new List<BarycenterEntry>
        {
            new() { V = "a" },
            new() { V = "b", Barycenter = 1, Weight = 2 }
        };
        constraintGraph.SetEdge("b", "a");

        var results = ResolveConflicts.Run(input, constraintGraph);
        await Assert.That(results.Count).IsEqualTo(1);
        await Assert.That(Join(results[0].Vs)).IsEqualTo("b,a");
        await Assert.That(results[0].I).IsEqualTo(0);
        await Assert.That(results[0].Barycenter!.Value).IsEqualTo(1).Within(0.001);
        await Assert.That(results[0].Weight!.Value).IsEqualTo(2).Within(0.001);
    }

    [Test]
    public async Task IgnoresEdgesNotRelatedToEntries()
    {
        var input = new List<BarycenterEntry>
        {
            new() { V = "a", Barycenter = 2, Weight = 3 },
            new() { V = "b", Barycenter = 1, Weight = 2 }
        };
        constraintGraph.SetEdge("c", "d");

        var results = SortByFirstV(ResolveConflicts.Run(input, constraintGraph));
        await Assert.That(results.Count).IsEqualTo(2);

        await Assert.That(results[0].Vs).IsEquivalentTo(new List<string> { "a" });
        await Assert.That(results[0].I).IsEqualTo(0);
        await Assert.That(results[0].Barycenter!.Value).IsEqualTo(2).Within(0.001);
        await Assert.That(results[0].Weight!.Value).IsEqualTo(3).Within(0.001);

        await Assert.That(results[1].Vs).IsEquivalentTo(new List<string> { "b" });
        await Assert.That(results[1].I).IsEqualTo(1);
        await Assert.That(results[1].Barycenter!.Value).IsEqualTo(1).Within(0.001);
        await Assert.That(results[1].Weight!.Value).IsEqualTo(2).Within(0.001);
    }
}
