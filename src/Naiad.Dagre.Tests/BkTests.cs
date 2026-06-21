namespace Naiad.Dagre.Tests;

// Ported from .dagre-ref/dagre/test/position/bk-test.ts
public class BkTests
{
    Graph g = null!;

    [Before(Test)]
    public void Setup() =>
        // The bk functions read graphLabel.nodesep/edgesep eagerly when building the block graph.
        // In JS these are undefined unless a test sets them; the tests that exercise separation always
        // assign them explicitly. We seed dagre's defaults so the unused values don't NPE in C#.
        g = new Graph().SetGraph(new GraphLabel { Nodesep = 50, Edgesep = 20, Ranksep = 50 });

    // ---- helpers ----------------------------------------------------------

    static Dictionary<string, string> StrMap(params (string K, string V)[] entries)
    {
        var d = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (k, v) in entries)
        {
            d[k] = v;
        }

        return d;
    }

    static Dictionary<string, double> NumMap(params (string K, double V)[] entries)
    {
        var d = new Dictionary<string, double>(StringComparer.Ordinal);
        foreach (var (k, v) in entries)
        {
            d[k] = v;
        }

        return d;
    }

    static async Task AssertStrMapEqual(Dictionary<string, string> actual, Dictionary<string, string> expected)
    {
        await Assert.That(actual.Count).IsEqualTo(expected.Count);
        foreach (var (k, v) in expected)
        {
            await Assert.That(actual.ContainsKey(k)).IsTrue();
            await Assert.That(actual[k]).IsEqualTo(v);
        }
    }

    static async Task AssertNumMapEqual(Dictionary<string, double> actual, Dictionary<string, double> expected)
    {
        await Assert.That(actual.Count).IsEqualTo(expected.Count);
        foreach (var (k, v) in expected)
        {
            await Assert.That(actual.ContainsKey(k)).IsTrue();
            await Assert.That(actual[k]).IsEqualTo(v).Within(0.0001);
        }
    }

    static List<string> Preds(Graph graph, string v) => graph.Predecessors(v) ?? [];

    // ======================================================================
    // findType1Conflicts
    // ======================================================================
    public class FindType1Conflicts
    {
        Graph g = null!;
        List<List<string>> layering = null!;

        [Before(Test)]
        public void Setup()
        {
            g = new Graph().SetGraph(new GraphLabel { Nodesep = 50, Edgesep = 20, Ranksep = 50 });
            g.SetDefaultEdgeLabel(new EdgeLabel());
            g.SetNode("a", new NodeLabel { Rank = 0, Order = 0 });
            g.SetNode("b", new NodeLabel { Rank = 0, Order = 1 });
            g.SetNode("c", new NodeLabel { Rank = 1, Order = 0 });
            g.SetNode("d", new NodeLabel { Rank = 1, Order = 1 });
            // Set up crossing
            g.SetEdge("a", "d");
            g.SetEdge("b", "c");

            layering = Util.BuildLayerMatrix(g);
        }

        [Test]
        public async Task DoesNotMarkEdgesThatHaveNoConflict()
        {
            g.RemoveEdge("a", "d");
            g.RemoveEdge("b", "c");
            g.SetEdge("a", "c");
            g.SetEdge("b", "d");

            var conflicts = BK.FindType1Conflicts(g, layering);
            await Assert.That(BK.HasConflict(conflicts, "a", "c")).IsFalse();
            await Assert.That(BK.HasConflict(conflicts, "b", "d")).IsFalse();
        }

        [Test]
        public async Task DoesNotMarkType0ConflictsNoDummies()
        {
            var conflicts = BK.FindType1Conflicts(g, layering);
            await Assert.That(BK.HasConflict(conflicts, "a", "d")).IsFalse();
            await Assert.That(BK.HasConflict(conflicts, "b", "c")).IsFalse();
        }

        [Test]
        [Arguments("a")]
        [Arguments("b")]
        [Arguments("c")]
        [Arguments("d")]
        public async Task DoesNotMarkType0ConflictsWhenOneIsDummy(string v)
        {
            g.Node(v).Dummy = "true";

            var conflicts = BK.FindType1Conflicts(g, layering);
            await Assert.That(BK.HasConflict(conflicts, "a", "d")).IsFalse();
            await Assert.That(BK.HasConflict(conflicts, "b", "c")).IsFalse();
        }

        [Test]
        [Arguments("a")]
        [Arguments("b")]
        [Arguments("c")]
        [Arguments("d")]
        public async Task DoesMarkType1ConflictsWhenOneIsNonDummy(string v)
        {
            foreach (var w in new[] { "a", "b", "c", "d" })
            {
                if (v != w)
                {
                    g.Node(w).Dummy = "true";
                }
            }

            var conflicts = BK.FindType1Conflicts(g, layering);
            if (v == "a" || v == "d")
            {
                await Assert.That(BK.HasConflict(conflicts, "a", "d")).IsTrue();
                await Assert.That(BK.HasConflict(conflicts, "b", "c")).IsFalse();
            }
            else
            {
                await Assert.That(BK.HasConflict(conflicts, "a", "d")).IsFalse();
                await Assert.That(BK.HasConflict(conflicts, "b", "c")).IsTrue();
            }
        }

        [Test]
        public async Task DoesNotMarkType2ConflictsAllDummies()
        {
            foreach (var v in new[] { "a", "b", "c", "d" })
            {
                g.Node(v).Dummy = "true";
            }

            var conflicts = BK.FindType1Conflicts(g, layering);
            await Assert.That(BK.HasConflict(conflicts, "a", "d")).IsFalse();
            await Assert.That(BK.HasConflict(conflicts, "b", "c")).IsFalse();
            BK.FindType1Conflicts(g, layering);
        }
    }

    // ======================================================================
    // findType2Conflicts
    // ======================================================================
    public class FindType2Conflicts
    {
        Graph g = null!;
        List<List<string>> layering = null!;

        [Before(Test)]
        public void Setup()
        {
            g = new Graph().SetGraph(new GraphLabel { Nodesep = 50, Edgesep = 20, Ranksep = 50 });
            g.SetDefaultEdgeLabel(new EdgeLabel());
            g.SetNode("a", new NodeLabel { Rank = 0, Order = 0 });
            g.SetNode("b", new NodeLabel { Rank = 0, Order = 1 });
            g.SetNode("c", new NodeLabel { Rank = 1, Order = 0 });
            g.SetNode("d", new NodeLabel { Rank = 1, Order = 1 });
            // Set up crossing
            g.SetEdge("a", "d");
            g.SetEdge("b", "c");

            layering = Util.BuildLayerMatrix(g);
        }

        [Test]
        public async Task MarksType2ConflictsFavoringBorderSegments1()
        {
            foreach (var v in new[] { "a", "d" })
            {
                g.Node(v).Dummy = "true";
            }

            foreach (var v in new[] { "b", "c" })
            {
                g.Node(v).Dummy = "border";
            }

            var conflicts = BK.FindType2Conflicts(g, layering);
            await Assert.That(BK.HasConflict(conflicts, "a", "d")).IsTrue();
            await Assert.That(BK.HasConflict(conflicts, "b", "c")).IsFalse();
            BK.FindType1Conflicts(g, layering);
        }

        [Test]
        public async Task MarksType2ConflictsFavoringBorderSegments2()
        {
            foreach (var v in new[] { "b", "c" })
            {
                g.Node(v).Dummy = "true";
            }

            foreach (var v in new[] { "a", "d" })
            {
                g.Node(v).Dummy = "border";
            }

            var conflicts = BK.FindType2Conflicts(g, layering);
            await Assert.That(BK.HasConflict(conflicts, "a", "d")).IsFalse();
            await Assert.That(BK.HasConflict(conflicts, "b", "c")).IsTrue();
            BK.FindType1Conflicts(g, layering);
        }
    }

    // ======================================================================
    // hasConflict
    // ======================================================================
    public class HasConflict
    {
        [Test]
        public async Task CanTestForAType1ConflictRegardlessOfEdgeOrientation()
        {
            var conflicts = new Dictionary<string, Dictionary<string, bool>>(StringComparer.Ordinal);
            BK.AddConflict(conflicts, "b", "a");
            await Assert.That(BK.HasConflict(conflicts, "a", "b")).IsTrue();
            await Assert.That(BK.HasConflict(conflicts, "b", "a")).IsTrue();
        }

        [Test]
        public async Task WorksForMultipleConflictsWithTheSameNode()
        {
            var conflicts = new Dictionary<string, Dictionary<string, bool>>(StringComparer.Ordinal);
            BK.AddConflict(conflicts, "a", "b");
            BK.AddConflict(conflicts, "a", "c");
            await Assert.That(BK.HasConflict(conflicts, "a", "b")).IsTrue();
            await Assert.That(BK.HasConflict(conflicts, "a", "c")).IsTrue();
        }
    }

    // ======================================================================
    // verticalAlignment
    // ======================================================================
    public class VerticalAlignment
    {
        Graph g = null!;

        [Before(Test)]
        public void Setup() =>
            g = new Graph().SetGraph(new GraphLabel { Nodesep = 50, Edgesep = 20, Ranksep = 50 });

        [Test]
        public async Task AlignsWithItselfIfTheNodeHasNoAdjacencies()
        {
            g.SetNode("a", new NodeLabel { Rank = 0, Order = 0 });
            g.SetNode("b", new NodeLabel { Rank = 1, Order = 0 });

            var layering = Util.BuildLayerMatrix(g);
            var conflicts = new Dictionary<string, Dictionary<string, bool>>(StringComparer.Ordinal);

            var result = BK.VerticalAlignment(g, layering, conflicts, v => g.Predecessors(v) ?? []);
            await AssertStrMapEqual(result.Root, StrMap(("a", "a"), ("b", "b")));
            await AssertStrMapEqual(result.Align, StrMap(("a", "a"), ("b", "b")));
        }

        [Test]
        public async Task AlignsWithItsSoleAdjacency()
        {
            g.SetNode("a", new NodeLabel { Rank = 0, Order = 0 });
            g.SetNode("b", new NodeLabel { Rank = 1, Order = 0 });
            g.SetEdge("a", "b");

            var layering = Util.BuildLayerMatrix(g);
            var conflicts = new Dictionary<string, Dictionary<string, bool>>(StringComparer.Ordinal);

            var result = BK.VerticalAlignment(g, layering, conflicts, v => g.Predecessors(v) ?? []);
            await AssertStrMapEqual(result.Root, StrMap(("a", "a"), ("b", "a")));
            await AssertStrMapEqual(result.Align, StrMap(("a", "b"), ("b", "a")));
        }

        [Test]
        public async Task AlignsWithItsLeftMedianWhenPossible()
        {
            g.SetNode("a", new NodeLabel { Rank = 0, Order = 0 });
            g.SetNode("b", new NodeLabel { Rank = 0, Order = 1 });
            g.SetNode("c", new NodeLabel { Rank = 1, Order = 0 });
            g.SetEdge("a", "c");
            g.SetEdge("b", "c");

            var layering = Util.BuildLayerMatrix(g);
            var conflicts = new Dictionary<string, Dictionary<string, bool>>(StringComparer.Ordinal);

            var result = BK.VerticalAlignment(g, layering, conflicts, v => g.Predecessors(v) ?? []);
            await AssertStrMapEqual(result.Root, StrMap(("a", "a"), ("b", "b"), ("c", "a")));
            await AssertStrMapEqual(result.Align, StrMap(("a", "c"), ("b", "b"), ("c", "a")));
        }

        [Test]
        public async Task AlignsCorrectlyRegardlessOfNodeNameOrInsertionOrder()
        {
            // This test ensures that we're actually properly sorting nodes by
            // position when searching for candidates.
            g.SetNode("b", new NodeLabel { Rank = 0, Order = 1 });
            g.SetNode("c", new NodeLabel { Rank = 1, Order = 0 });
            g.SetNode("z", new NodeLabel { Rank = 0, Order = 0 });
            g.SetEdge("z", "c");
            g.SetEdge("b", "c");

            var layering = Util.BuildLayerMatrix(g);
            var conflicts = new Dictionary<string, Dictionary<string, bool>>(StringComparer.Ordinal);

            var result = BK.VerticalAlignment(g, layering, conflicts, v => g.Predecessors(v) ?? []);
            await AssertStrMapEqual(result.Root, StrMap(("z", "z"), ("b", "b"), ("c", "z")));
            await AssertStrMapEqual(result.Align, StrMap(("z", "c"), ("b", "b"), ("c", "z")));
        }

        [Test]
        public async Task AlignsWithItsRightMedianWhenLeftIsUnavailable()
        {
            g.SetNode("a", new NodeLabel { Rank = 0, Order = 0 });
            g.SetNode("b", new NodeLabel { Rank = 0, Order = 1 });
            g.SetNode("c", new NodeLabel { Rank = 1, Order = 0 });
            g.SetEdge("a", "c");
            g.SetEdge("b", "c");

            var layering = Util.BuildLayerMatrix(g);
            var conflicts = new Dictionary<string, Dictionary<string, bool>>(StringComparer.Ordinal);

            BK.AddConflict(conflicts, "a", "c");

            var result = BK.VerticalAlignment(g, layering, conflicts, v => g.Predecessors(v) ?? []);
            await AssertStrMapEqual(result.Root, StrMap(("a", "a"), ("b", "b"), ("c", "b")));
            await AssertStrMapEqual(result.Align, StrMap(("a", "a"), ("b", "c"), ("c", "b")));
        }

        [Test]
        public async Task AlignsWithNeitherMedianIfBothAreUnavailable()
        {
            g.SetNode("a", new NodeLabel { Rank = 0, Order = 0 });
            g.SetNode("b", new NodeLabel { Rank = 0, Order = 1 });
            g.SetNode("c", new NodeLabel { Rank = 1, Order = 0 });
            g.SetNode("d", new NodeLabel { Rank = 1, Order = 1 });
            g.SetEdge("a", "d");
            g.SetEdge("b", "c");
            g.SetEdge("b", "d");

            var layering = Util.BuildLayerMatrix(g);
            var conflicts = new Dictionary<string, Dictionary<string, bool>>(StringComparer.Ordinal);

            var result = BK.VerticalAlignment(g, layering, conflicts, v => g.Predecessors(v) ?? []);
            // c will align with b, so d will not be able to align with a, because
            // (a,d) and (c,b) cross.
            await AssertStrMapEqual(result.Root, StrMap(("a", "a"), ("b", "b"), ("c", "b"), ("d", "d")));
            await AssertStrMapEqual(result.Align, StrMap(("a", "a"), ("b", "c"), ("c", "b"), ("d", "d")));
        }

        [Test]
        public async Task AlignsWithTheSingleMedianForAnOddNumberOfAdjacencies()
        {
            g.SetNode("a", new NodeLabel { Rank = 0, Order = 0 });
            g.SetNode("b", new NodeLabel { Rank = 0, Order = 1 });
            g.SetNode("c", new NodeLabel { Rank = 0, Order = 2 });
            g.SetNode("d", new NodeLabel { Rank = 1, Order = 0 });
            g.SetEdge("a", "d");
            g.SetEdge("b", "d");
            g.SetEdge("c", "d");

            var layering = Util.BuildLayerMatrix(g);
            var conflicts = new Dictionary<string, Dictionary<string, bool>>(StringComparer.Ordinal);

            var result = BK.VerticalAlignment(g, layering, conflicts, v => g.Predecessors(v) ?? []);
            await AssertStrMapEqual(result.Root, StrMap(("a", "a"), ("b", "b"), ("c", "c"), ("d", "b")));
            await AssertStrMapEqual(result.Align, StrMap(("a", "a"), ("b", "d"), ("c", "c"), ("d", "b")));
        }

        [Test]
        public async Task AlignsBlocksAcrossMultipleLayers()
        {
            g.SetNode("a", new NodeLabel { Rank = 0, Order = 0 });
            g.SetNode("b", new NodeLabel { Rank = 1, Order = 0 });
            g.SetNode("c", new NodeLabel { Rank = 1, Order = 1 });
            g.SetNode("d", new NodeLabel { Rank = 2, Order = 0 });
            g.SetPath(new[] { "a", "b", "d" });
            g.SetPath(new[] { "a", "c", "d" });

            var layering = Util.BuildLayerMatrix(g);
            var conflicts = new Dictionary<string, Dictionary<string, bool>>(StringComparer.Ordinal);

            var result = BK.VerticalAlignment(g, layering, conflicts, v => g.Predecessors(v) ?? []);
            await AssertStrMapEqual(result.Root, StrMap(("a", "a"), ("b", "a"), ("c", "c"), ("d", "a")));
            await AssertStrMapEqual(result.Align, StrMap(("a", "b"), ("b", "d"), ("c", "c"), ("d", "a")));
        }
    }

    // ======================================================================
    // horizontalCompaction
    // ======================================================================
    public class HorizontalCompaction
    {
        Graph g = null!;

        [Before(Test)]
        public void Setup() =>
            g = new Graph().SetGraph(new GraphLabel { Nodesep = 50, Edgesep = 20, Ranksep = 50 });

        [Test]
        public async Task PlacesTheCenterOfASingleNodeGraphAtOrigin()
        {
            var root = StrMap(("a", "a"));
            var align = StrMap(("a", "a"));
            g.SetNode("a", new NodeLabel { Rank = 0, Order = 0 });

            var xs = BK.HorizontalCompaction(g, Util.BuildLayerMatrix(g), root, align, false);
            await Assert.That(xs["a"]).IsEqualTo(0d);
        }

        [Test]
        public async Task SeparatesAdjacentNodesBySpecifiedNodeSeparation()
        {
            var root = StrMap(("a", "a"), ("b", "b"));
            var align = StrMap(("a", "a"), ("b", "b"));
            g.Graph_().Nodesep = 100;
            g.SetNode("a", new NodeLabel { Rank = 0, Order = 0, Width = 100 });
            g.SetNode("b", new NodeLabel { Rank = 0, Order = 1, Width = 200 });

            var xs = BK.HorizontalCompaction(g, Util.BuildLayerMatrix(g), root, align, false);
            await Assert.That(xs["a"]).IsEqualTo(0d);
            await Assert.That(xs["b"]).IsEqualTo(100 / 2.0 + 100 + 200 / 2.0);
        }

        [Test]
        public async Task SeparatesAdjacentEdgesBySpecifiedNodeSeparation()
        {
            var root = StrMap(("a", "a"), ("b", "b"));
            var align = StrMap(("a", "a"), ("b", "b"));
            g.Graph_().Edgesep = 20;
            g.SetNode("a", new NodeLabel { Rank = 0, Order = 0, Width = 100, Dummy = "true" });
            g.SetNode("b", new NodeLabel { Rank = 0, Order = 1, Width = 200, Dummy = "true" });

            var xs = BK.HorizontalCompaction(g, Util.BuildLayerMatrix(g), root, align, false);
            await Assert.That(xs["a"]).IsEqualTo(0d);
            await Assert.That(xs["b"]).IsEqualTo(100 / 2.0 + 20 + 200 / 2.0);
        }

        [Test]
        public async Task AlignsTheCentersOfNodesInTheSameBlock()
        {
            var root = StrMap(("a", "a"), ("b", "a"));
            var align = StrMap(("a", "b"), ("b", "a"));
            g.SetNode("a", new NodeLabel { Rank = 0, Order = 0, Width = 100 });
            g.SetNode("b", new NodeLabel { Rank = 1, Order = 0, Width = 200 });

            var xs = BK.HorizontalCompaction(g, Util.BuildLayerMatrix(g), root, align, false);
            await Assert.That(xs["a"]).IsEqualTo(0d);
            await Assert.That(xs["b"]).IsEqualTo(0d);
        }

        [Test]
        public async Task SeparatesBlocksWithTheAppropriateSeparation()
        {
            var root = StrMap(("a", "a"), ("b", "a"), ("c", "c"));
            var align = StrMap(("a", "b"), ("b", "a"), ("c", "c"));
            g.Graph_().Nodesep = 75;
            g.SetNode("a", new NodeLabel { Rank = 0, Order = 0, Width = 100 });
            g.SetNode("b", new NodeLabel { Rank = 1, Order = 1, Width = 200 });
            g.SetNode("c", new NodeLabel { Rank = 1, Order = 0, Width = 50 });

            var xs = BK.HorizontalCompaction(g, Util.BuildLayerMatrix(g), root, align, false);
            await Assert.That(xs["a"]).IsEqualTo(50 / 2.0 + 75 + 200 / 2.0);
            await Assert.That(xs["b"]).IsEqualTo(50 / 2.0 + 75 + 200 / 2.0);
            await Assert.That(xs["c"]).IsEqualTo(0d);
        }

        [Test]
        public async Task SeparatesClassesWithTheAppropriateSeparation()
        {
            var root = StrMap(("a", "a"), ("b", "b"), ("c", "c"), ("d", "b"));
            var align = StrMap(("a", "a"), ("b", "d"), ("c", "c"), ("d", "b"));
            g.Graph_().Nodesep = 75;
            g.SetNode("a", new NodeLabel { Rank = 0, Order = 0, Width = 100 });
            g.SetNode("b", new NodeLabel { Rank = 0, Order = 1, Width = 200 });
            g.SetNode("c", new NodeLabel { Rank = 1, Order = 0, Width = 50 });
            g.SetNode("d", new NodeLabel { Rank = 1, Order = 1, Width = 80 });

            var xs = BK.HorizontalCompaction(g, Util.BuildLayerMatrix(g), root, align, false);
            await Assert.That(xs["a"]).IsEqualTo(0d);
            await Assert.That(xs["b"]).IsEqualTo(100 / 2.0 + 75 + 200 / 2.0);
            await Assert.That(xs["c"]).IsEqualTo(100 / 2.0 + 75 + 200 / 2.0 - 80 / 2.0 - 75 - 50 / 2.0);
            await Assert.That(xs["d"]).IsEqualTo(100 / 2.0 + 75 + 200 / 2.0);
        }

        [Test]
        public async Task ShiftsClassesByMaxSepFromTheAdjacentBlock1()
        {
            var root = StrMap(("a", "a"), ("b", "b"), ("c", "a"), ("d", "b"));
            var align = StrMap(("a", "c"), ("b", "d"), ("c", "a"), ("d", "b"));
            g.Graph_().Nodesep = 75;
            g.SetNode("a", new NodeLabel { Rank = 0, Order = 0, Width = 50 });
            g.SetNode("b", new NodeLabel { Rank = 0, Order = 1, Width = 150 });
            g.SetNode("c", new NodeLabel { Rank = 1, Order = 0, Width = 60 });
            g.SetNode("d", new NodeLabel { Rank = 1, Order = 1, Width = 70 });

            var xs = BK.HorizontalCompaction(g, Util.BuildLayerMatrix(g), root, align, false);
            await Assert.That(xs["a"]).IsEqualTo(0d);
            await Assert.That(xs["b"]).IsEqualTo(50 / 2.0 + 75 + 150 / 2.0);
            await Assert.That(xs["c"]).IsEqualTo(0d);
            await Assert.That(xs["d"]).IsEqualTo(50 / 2.0 + 75 + 150 / 2.0);
        }

        [Test]
        public async Task ShiftsClassesByMaxSepFromTheAdjacentBlock2()
        {
            var root = StrMap(("a", "a"), ("b", "b"), ("c", "a"), ("d", "b"));
            var align = StrMap(("a", "c"), ("b", "d"), ("c", "a"), ("d", "b"));
            g.Graph_().Nodesep = 75;
            g.SetNode("a", new NodeLabel { Rank = 0, Order = 0, Width = 50 });
            g.SetNode("b", new NodeLabel { Rank = 0, Order = 1, Width = 70 });
            g.SetNode("c", new NodeLabel { Rank = 1, Order = 0, Width = 60 });
            g.SetNode("d", new NodeLabel { Rank = 1, Order = 1, Width = 150 });

            var xs = BK.HorizontalCompaction(g, Util.BuildLayerMatrix(g), root, align, false);
            await Assert.That(xs["a"]).IsEqualTo(0d);
            await Assert.That(xs["b"]).IsEqualTo(60 / 2.0 + 75 + 150 / 2.0);
            await Assert.That(xs["c"]).IsEqualTo(0d);
            await Assert.That(xs["d"]).IsEqualTo(60 / 2.0 + 75 + 150 / 2.0);
        }

        [Test]
        public async Task CascadesClassShift()
        {
            var root = StrMap(("a", "a"), ("b", "b"), ("c", "c"), ("d", "d"), ("e", "b"), ("f", "f"), ("g", "d"));
            var align = StrMap(("a", "a"), ("b", "e"), ("c", "c"), ("d", "g"), ("e", "b"), ("f", "f"), ("g", "d"));
            g.Graph_().Nodesep = 75;
            g.SetNode("a", new NodeLabel { Rank = 0, Order = 0, Width = 50 });
            g.SetNode("b", new NodeLabel { Rank = 0, Order = 1, Width = 50 });
            g.SetNode("c", new NodeLabel { Rank = 1, Order = 0, Width = 50 });
            g.SetNode("d", new NodeLabel { Rank = 1, Order = 1, Width = 50 });
            g.SetNode("e", new NodeLabel { Rank = 1, Order = 2, Width = 50 });
            g.SetNode("f", new NodeLabel { Rank = 2, Order = 0, Width = 50 });
            g.SetNode("g", new NodeLabel { Rank = 2, Order = 1, Width = 50 });

            var xs = BK.HorizontalCompaction(g, Util.BuildLayerMatrix(g), root, align, false);

            // Use f as 0, everything is relative to it
            await Assert.That(xs["a"]).IsEqualTo(xs["b"] - 50 / 2.0 - 75 - 50 / 2.0);
            await Assert.That(xs["b"]).IsEqualTo(xs["e"]);
            await Assert.That(xs["c"]).IsEqualTo(xs["f"]);
            await Assert.That(xs["d"]).IsEqualTo(xs["c"] + 50 / 2.0 + 75 + 50 / 2.0);
            await Assert.That(xs["e"]).IsEqualTo(xs["d"] + 50 / 2.0 + 75 + 50 / 2.0);
            await Assert.That(xs["g"]).IsEqualTo(xs["f"] + 50 / 2.0 + 75 + 50 / 2.0);
        }

        [Test]
        public async Task HandlesLabelposL()
        {
            var root = StrMap(("a", "a"), ("b", "b"), ("c", "c"));
            var align = StrMap(("a", "a"), ("b", "b"), ("c", "c"));
            g.Graph_().Edgesep = 50;
            g.SetNode("a", new NodeLabel { Rank = 0, Order = 0, Width = 100, Dummy = "edge" });
            g.SetNode("b", new NodeLabel { Rank = 0, Order = 1, Width = 200, Dummy = "edge-label", Labelpos = "l" });
            g.SetNode("c", new NodeLabel { Rank = 0, Order = 2, Width = 300, Dummy = "edge" });

            var xs = BK.HorizontalCompaction(g, Util.BuildLayerMatrix(g), root, align, false);
            await Assert.That(xs["a"]).IsEqualTo(0d);
            await Assert.That(xs["b"]).IsEqualTo(xs["a"] + 100 / 2.0 + 50 + 200);
            await Assert.That(xs["c"]).IsEqualTo(xs["b"] + 0 + 50 + 300 / 2.0);
        }

        [Test]
        public async Task HandlesLabelposC()
        {
            var root = StrMap(("a", "a"), ("b", "b"), ("c", "c"));
            var align = StrMap(("a", "a"), ("b", "b"), ("c", "c"));
            g.Graph_().Edgesep = 50;
            g.SetNode("a", new NodeLabel { Rank = 0, Order = 0, Width = 100, Dummy = "edge" });
            g.SetNode("b", new NodeLabel { Rank = 0, Order = 1, Width = 200, Dummy = "edge-label", Labelpos = "c" });
            g.SetNode("c", new NodeLabel { Rank = 0, Order = 2, Width = 300, Dummy = "edge" });

            var xs = BK.HorizontalCompaction(g, Util.BuildLayerMatrix(g), root, align, false);
            await Assert.That(xs["a"]).IsEqualTo(0d);
            await Assert.That(xs["b"]).IsEqualTo(xs["a"] + 100 / 2.0 + 50 + 200 / 2.0);
            await Assert.That(xs["c"]).IsEqualTo(xs["b"] + 200 / 2.0 + 50 + 300 / 2.0);
        }

        [Test]
        public async Task HandlesLabelposR()
        {
            var root = StrMap(("a", "a"), ("b", "b"), ("c", "c"));
            var align = StrMap(("a", "a"), ("b", "b"), ("c", "c"));
            g.Graph_().Edgesep = 50;
            g.SetNode("a", new NodeLabel { Rank = 0, Order = 0, Width = 100, Dummy = "edge" });
            g.SetNode("b", new NodeLabel { Rank = 0, Order = 1, Width = 200, Dummy = "edge-label", Labelpos = "r" });
            g.SetNode("c", new NodeLabel { Rank = 0, Order = 2, Width = 300, Dummy = "edge" });

            var xs = BK.HorizontalCompaction(g, Util.BuildLayerMatrix(g), root, align, false);
            await Assert.That(xs["a"]).IsEqualTo(0d);
            await Assert.That(xs["b"]).IsEqualTo(xs["a"] + 100 / 2.0 + 50 + 0);
            await Assert.That(xs["c"]).IsEqualTo(xs["b"] + 200 + 50 + 300 / 2.0);
        }
    }

    // ======================================================================
    // alignCoordinates
    // ======================================================================
    public class AlignCoordinates
    {
        [Test]
        public async Task AlignsASingleNode()
        {
            var ul = NumMap(("a", 50));
            var ur = NumMap(("a", 100));
            var dl = NumMap(("a", 50));
            var dr = NumMap(("a", 200));
            var xss = new Dictionary<string, Dictionary<string, double>>(StringComparer.Ordinal)
            {
                ["ul"] = ul,
                ["ur"] = ur,
                ["dl"] = dl,
                ["dr"] = dr
            };

            BK.AlignCoordinates(xss, ul);

            await AssertNumMapEqual(xss["ul"], NumMap(("a", 50)));
            await AssertNumMapEqual(xss["ur"], NumMap(("a", 50)));
            await AssertNumMapEqual(xss["dl"], NumMap(("a", 50)));
            await AssertNumMapEqual(xss["dr"], NumMap(("a", 50)));
        }

        [Test]
        public async Task AlignsMultipleNodes()
        {
            var ul = NumMap(("a", 50), ("b", 1000));
            var ur = NumMap(("a", 100), ("b", 900));
            var dl = NumMap(("a", 150), ("b", 800));
            var dr = NumMap(("a", 200), ("b", 700));
            var xss = new Dictionary<string, Dictionary<string, double>>(StringComparer.Ordinal)
            {
                ["ul"] = ul,
                ["ur"] = ur,
                ["dl"] = dl,
                ["dr"] = dr
            };

            BK.AlignCoordinates(xss, ul);

            await AssertNumMapEqual(xss["ul"], NumMap(("a", 50), ("b", 1000)));
            await AssertNumMapEqual(xss["ur"], NumMap(("a", 200), ("b", 1000)));
            await AssertNumMapEqual(xss["dl"], NumMap(("a", 50), ("b", 700)));
            await AssertNumMapEqual(xss["dr"], NumMap(("a", 500), ("b", 1000)));
        }
    }

    // ======================================================================
    // findSmallestWidthAlignment
    // ======================================================================
    public class FindSmallestWidthAlignment
    {
        Graph g = null!;

        [Before(Test)]
        public void Setup() =>
            g = new Graph().SetGraph(new GraphLabel { Nodesep = 50, Edgesep = 20, Ranksep = 50 });

        [Test]
        public async Task FindsTheAlignmentWithTheSmallestWidth()
        {
            g.SetNode("a", new NodeLabel { Width = 50 });
            g.SetNode("b", new NodeLabel { Width = 50 });

            var xss = new Dictionary<string, Dictionary<string, double>>(StringComparer.Ordinal)
            {
                ["ul"] = NumMap(("a", 0), ("b", 1000)),
                ["ur"] = NumMap(("a", -5), ("b", 1000)),
                ["dl"] = NumMap(("a", 5), ("b", 2000)),
                ["dr"] = NumMap(("a", 0), ("b", 200))
            };

            var result = BK.FindSmallestWidthAlignment(g, xss);
            await AssertNumMapEqual(result, NumMap(("a", 0), ("b", 200)));
        }

        [Test]
        public async Task TakesNodeWidthIntoAccount()
        {
            g.SetNode("a", new NodeLabel { Width = 50 });
            g.SetNode("b", new NodeLabel { Width = 50 });
            g.SetNode("c", new NodeLabel { Width = 200 });

            var xss = new Dictionary<string, Dictionary<string, double>>(StringComparer.Ordinal)
            {
                ["ul"] = NumMap(("a", 0), ("b", 100), ("c", 75)),
                ["ur"] = NumMap(("a", 0), ("b", 100), ("c", 80)),
                ["dl"] = NumMap(("a", 0), ("b", 100), ("c", 85)),
                ["dr"] = NumMap(("a", 0), ("b", 100), ("c", 90))
            };

            var result = BK.FindSmallestWidthAlignment(g, xss);
            await AssertNumMapEqual(result, NumMap(("a", 0), ("b", 100), ("c", 75)));
        }
    }

    // ======================================================================
    // balance
    // ======================================================================
    public class Balance
    {
        [Test]
        public async Task AlignsASingleNodeToTheSharedMedianValue()
        {
            var xss = new Dictionary<string, Dictionary<string, double>>(StringComparer.Ordinal)
            {
                ["ul"] = NumMap(("a", 0)),
                ["ur"] = NumMap(("a", 100)),
                ["dl"] = NumMap(("a", 100)),
                ["dr"] = NumMap(("a", 200))
            };

            await AssertNumMapEqual(BK.Balance(xss), NumMap(("a", 100)));
        }

        [Test]
        public async Task AlignsASingleNodeToTheAverageOfDifferentMedianValues()
        {
            var xss = new Dictionary<string, Dictionary<string, double>>(StringComparer.Ordinal)
            {
                ["ul"] = NumMap(("a", 0)),
                ["ur"] = NumMap(("a", 75)),
                ["dl"] = NumMap(("a", 125)),
                ["dr"] = NumMap(("a", 200))
            };

            await AssertNumMapEqual(BK.Balance(xss), NumMap(("a", 100)));
        }

        [Test]
        public async Task BalancesMultipleNodes()
        {
            var xss = new Dictionary<string, Dictionary<string, double>>(StringComparer.Ordinal)
            {
                ["ul"] = NumMap(("a", 0), ("b", 50)),
                ["ur"] = NumMap(("a", 75), ("b", 0)),
                ["dl"] = NumMap(("a", 125), ("b", 60)),
                ["dr"] = NumMap(("a", 200), ("b", 75))
            };

            await AssertNumMapEqual(BK.Balance(xss), NumMap(("a", 100), ("b", 55)));
        }
    }

    // ======================================================================
    // positionX
    // ======================================================================
    public class PositionX
    {
        Graph g = null!;

        [Before(Test)]
        public void Setup() =>
            g = new Graph().SetGraph(new GraphLabel { Nodesep = 50, Edgesep = 20, Ranksep = 50 });

        [Test]
        public async Task PositionsASingleNodeAtOrigin()
        {
            g.SetNode("a", new NodeLabel { Rank = 0, Order = 0, Width = 100 });
            await AssertNumMapEqual(BK.PositionX(g), NumMap(("a", 0)));
        }

        [Test]
        public async Task PositionsASingleNodeBlockAtOrigin()
        {
            g.SetNode("a", new NodeLabel { Rank = 0, Order = 0, Width = 100 });
            g.SetNode("b", new NodeLabel { Rank = 1, Order = 0, Width = 100 });
            g.SetEdge("a", "b");
            await AssertNumMapEqual(BK.PositionX(g), NumMap(("a", 0), ("b", 0)));
        }

        [Test]
        public async Task PositionsASingleNodeBlockAtOriginEvenWhenTheirSizesDiffer()
        {
            g.SetNode("a", new NodeLabel { Rank = 0, Order = 0, Width = 40 });
            g.SetNode("b", new NodeLabel { Rank = 1, Order = 0, Width = 500 });
            g.SetNode("c", new NodeLabel { Rank = 2, Order = 0, Width = 20 });
            g.SetPath(new[] { "a", "b", "c" });
            await AssertNumMapEqual(BK.PositionX(g), NumMap(("a", 0), ("b", 0), ("c", 0)));
        }

        [Test]
        public async Task CentersANodeIfItIsAPredecessorOfTwoSameSizedNodes()
        {
            g.Graph_().Nodesep = 10;
            g.SetNode("a", new NodeLabel { Rank = 0, Order = 0, Width = 20 });
            g.SetNode("b", new NodeLabel { Rank = 1, Order = 0, Width = 50 });
            g.SetNode("c", new NodeLabel { Rank = 1, Order = 1, Width = 50 });
            g.SetEdge("a", "b");
            g.SetEdge("a", "c");

            var pos = BK.PositionX(g);
            var a = pos["a"];
            await AssertNumMapEqual(pos, NumMap(("a", a), ("b", a - (25 + 5)), ("c", a + (25 + 5))));
        }

        [Test]
        public async Task ShiftsBlocksOnBothSidesOfAlignedBlock()
        {
            g.Graph_().Nodesep = 10;
            g.SetNode("a", new NodeLabel { Rank = 0, Order = 0, Width = 50 });
            g.SetNode("b", new NodeLabel { Rank = 0, Order = 1, Width = 60 });
            g.SetNode("c", new NodeLabel { Rank = 1, Order = 0, Width = 70 });
            g.SetNode("d", new NodeLabel { Rank = 1, Order = 1, Width = 80 });
            g.SetEdge("b", "c");

            var pos = BK.PositionX(g);
            var b = pos["b"];
            var c = b;
            await AssertNumMapEqual(pos, NumMap(
                ("a", b - 60 / 2.0 - 10 - 50 / 2.0),
                ("b", b),
                ("c", c),
                ("d", c + 70 / 2.0 + 10 + 80 / 2.0)));
        }

        [Test]
        public async Task AlignsInnerSegments()
        {
            g.Graph_().Nodesep = 10;
            g.Graph_().Edgesep = 10;
            g.SetNode("a", new NodeLabel { Rank = 0, Order = 0, Width = 50, Dummy = "true" });
            g.SetNode("b", new NodeLabel { Rank = 0, Order = 1, Width = 60 });
            g.SetNode("c", new NodeLabel { Rank = 1, Order = 0, Width = 70 });
            g.SetNode("d", new NodeLabel { Rank = 1, Order = 1, Width = 80, Dummy = "true" });
            g.SetEdge("b", "c");
            g.SetEdge("a", "d");

            var pos = BK.PositionX(g);
            var a = pos["a"];
            var d = a;
            await AssertNumMapEqual(pos, NumMap(
                ("a", a),
                ("b", a + 50 / 2.0 + 10 + 60 / 2.0),
                ("c", d - 70 / 2.0 - 10 - 80 / 2.0),
                ("d", d)));
        }
    }
}
