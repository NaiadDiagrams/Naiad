namespace Naiad.Dagre.Tests;

// Port of dagre's test/normalize-test.ts.
public class NormalizeTests
{
    Graph graph = null!;

    [Before(Test)]
    public void Setup() =>
        graph = new Graph(multigraph: true, compound: true).SetGraph(new());

    static (string V, string W) IncidentNodes(Edge edge) => (edge.V, edge.W);

    public class RunTests : NormalizeTests
    {
        [Test]
        public async Task DoesNotChangeAShortEdge()
        {
            graph.SetNode("a", new() { Rank = 0 });
            graph.SetNode("b", new() { Rank = 1 });
            graph.SetEdge("a", "b", new());

            Normalize.Run(graph);

            var incident = graph.Edges().Select(IncidentNodes).ToList();
            await Assert.That(incident).IsEquivalentTo(new List<(string, string)> { ("a", "b") });
            await Assert.That(graph.NodeLabel("a").Rank).IsEqualTo(0);
            await Assert.That(graph.NodeLabel("b").Rank).IsEqualTo(1);
        }

        [Test]
        public async Task SplitsATwoLayerEdgeIntoTwoSegments()
        {
            graph.SetNode("a", new() { Rank = 0 });
            graph.SetNode("b", new() { Rank = 2 });
            graph.SetEdge("a", "b", new());

            Normalize.Run(graph);

            await Assert.That(graph.Successors("a")!.Count).IsEqualTo(1);
            var successor = graph.Successors("a")![0];
            await Assert.That(graph.NodeLabel(successor).Dummy).IsEqualTo("edge");
            await Assert.That(graph.NodeLabel(successor).Rank).IsEqualTo(1);
            await Assert.That(graph.Successors(successor)).IsEquivalentTo(new List<string> { "b" });
            await Assert.That(graph.NodeLabel("a").Rank).IsEqualTo(0);
            await Assert.That(graph.NodeLabel("b").Rank).IsEqualTo(2);

            await Assert.That(graph.Label.DummyChains!.Count).IsEqualTo(1);
            await Assert.That(graph.Label.DummyChains![0]).IsEqualTo(successor);
        }

        [Test]
        public async Task AssignsWidth0Height0ToDummyNodesByDefault()
        {
            graph.SetNode("a", new() { Rank = 0 });
            graph.SetNode("b", new() { Rank = 2 });
            graph.SetEdge("a", "b", new() { Width = 10, Height = 10 });

            Normalize.Run(graph);

            await Assert.That(graph.Successors("a")!.Count).IsEqualTo(1);
            var successor = graph.Successors("a")![0];
            await Assert.That(graph.NodeLabel(successor).Width).IsEqualTo(0);
            await Assert.That(graph.NodeLabel(successor).Height).IsEqualTo(0);
        }

        [Test]
        public async Task AssignsWidthAndHeightFromTheEdgeForTheNodeOnLabelRank()
        {
            graph.SetNode("a", new() { Rank = 0 });
            graph.SetNode("b", new() { Rank = 4 });
            graph.SetEdge("a", "b", new() { Width = 20, Height = 10, LabelRank = 2 });

            Normalize.Run(graph);

            var labelV = graph.Successors(graph.Successors("a")![0])![0];
            var labelNode = graph.NodeLabel(labelV);
            await Assert.That(labelNode.Width).IsEqualTo(20);
            await Assert.That(labelNode.Height).IsEqualTo(10);
        }

        [Test]
        public async Task PreservesTheWeightForTheEdge()
        {
            graph.SetNode("a", new() { Rank = 0 });
            graph.SetNode("b", new() { Rank = 2 });
            graph.SetEdge("a", "b", new() { Weight = 2 });

            Normalize.Run(graph);

            await Assert.That(graph.Successors("a")!.Count).IsEqualTo(1);
            await Assert.That(graph.FindEdgeLabel("a", graph.Successors("a")![0]).Weight).IsEqualTo(2);
        }
    }

    public class UndoTests : NormalizeTests
    {
        [Test]
        public async Task ReversesTheRunOperation()
        {
            graph.SetNode("a", new() { Rank = 0 });
            graph.SetNode("b", new() { Rank = 2 });
            graph.SetEdge("a", "b", new());

            Normalize.Run(graph);
            Normalize.Undo(graph);

            var incident = graph.Edges().Select(IncidentNodes).ToList();
            await Assert.That(incident).IsEquivalentTo(new List<(string, string)> { ("a", "b") });
            await Assert.That(graph.NodeLabel("a").Rank).IsEqualTo(0);
            await Assert.That(graph.NodeLabel("b").Rank).IsEqualTo(2);
        }

        [Test]
        public async Task RestoresPreviousEdgeLabels()
        {
            graph.SetNode("a", new() { Rank = 0 });
            graph.SetNode("b", new() { Rank = 2 });
            // The TS sets an arbitrary {foo: "bar"} property; the closest faithful equivalent
            // here is the ForwardName label field, asserting it survives run/undo.
            graph.SetEdge("a", "b", new() { ForwardName = "bar" });

            Normalize.Run(graph);
            Normalize.Undo(graph);

            await Assert.That(graph.FindEdgeLabel("a", "b").ForwardName).IsEqualTo("bar");
        }

        [Test]
        public async Task CollectsAssignedCoordinatesIntoThePointsAttribute()
        {
            graph.SetNode("a", new() { Rank = 0 });
            graph.SetNode("b", new() { Rank = 2 });
            graph.SetEdge("a", "b", new());

            Normalize.Run(graph);

            var dummyLabel = graph.NodeLabel(graph.Neighbors("a")![0]);
            dummyLabel.X = 5;
            dummyLabel.Y = 10;

            Normalize.Undo(graph);

            var points = graph.FindEdgeLabel("a", "b").Points!;
            await Assert.That(points.Count).IsEqualTo(1);
            await Assert.That(points[0].X).IsEqualTo(5);
            await Assert.That(points[0].Y).IsEqualTo(10);
        }

        [Test]
        public async Task MergesAssignedCoordinatesIntoThePointsAttribute()
        {
            graph.SetNode("a", new() { Rank = 0 });
            graph.SetNode("b", new() { Rank = 4 });
            graph.SetEdge("a", "b", new());

            Normalize.Run(graph);

            var aSucLabel = graph.NodeLabel(graph.Neighbors("a")![0]);
            aSucLabel.X = 5;
            aSucLabel.Y = 10;

            var midLabel = graph.NodeLabel(graph.Successors(graph.Successors("a")![0])![0]);
            midLabel.X = 20;
            midLabel.Y = 25;

            var bPredLabel = graph.NodeLabel(graph.Neighbors("b")![0]);
            bPredLabel.X = 100;
            bPredLabel.Y = 200;

            Normalize.Undo(graph);

            var points = graph.FindEdgeLabel("a", "b").Points!;
            await Assert.That(points.Count).IsEqualTo(3);
            await Assert.That(points[0].X).IsEqualTo(5);
            await Assert.That(points[0].Y).IsEqualTo(10);
            await Assert.That(points[1].X).IsEqualTo(20);
            await Assert.That(points[1].Y).IsEqualTo(25);
            await Assert.That(points[2].X).IsEqualTo(100);
            await Assert.That(points[2].Y).IsEqualTo(200);
        }

        [Test]
        public async Task SetsCoordsAndDimsForTheLabelIfTheEdgeHasOne()
        {
            graph.SetNode("a", new() { Rank = 0 });
            graph.SetNode("b", new() { Rank = 2 });
            graph.SetEdge("a", "b", new() { Width = 10, Height = 20, LabelRank = 1 });

            Normalize.Run(graph);

            var labelNode = graph.NodeLabel(graph.Successors("a")![0]);
            labelNode.X = 50;
            labelNode.Y = 60;
            labelNode.Width = 20;
            labelNode.Height = 10;

            Normalize.Undo(graph);

            var label = graph.FindEdgeLabel("a", "b");
            await Assert.That(label.X).IsEqualTo(50);
            await Assert.That(label.Y).IsEqualTo(60);
            await Assert.That(label.Width).IsEqualTo(20);
            await Assert.That(label.Height).IsEqualTo(10);
        }

        [Test]
        public async Task SetsCoordsAndDimsForTheLabelIfTheLongEdgeHasOne()
        {
            graph.SetNode("a", new() { Rank = 0 });
            graph.SetNode("b", new() { Rank = 4 });
            graph.SetEdge("a", "b", new() { Width = 10, Height = 20, LabelRank = 2 });

            Normalize.Run(graph);

            var labelNode = graph.NodeLabel(graph.Successors(graph.Successors("a")![0])![0]);
            labelNode.X = 50;
            labelNode.Y = 60;
            labelNode.Width = 20;
            labelNode.Height = 10;

            Normalize.Undo(graph);

            var label = graph.FindEdgeLabel("a", "b");
            await Assert.That(label.X).IsEqualTo(50);
            await Assert.That(label.Y).IsEqualTo(60);
            await Assert.That(label.Width).IsEqualTo(20);
            await Assert.That(label.Height).IsEqualTo(10);
        }

        [Test]
        public async Task RestoresMultiEdges()
        {
            graph.SetNode("a", new() { Rank = 0 });
            graph.SetNode("b", new() { Rank = 2 });
            graph.SetEdge("a", "b", new(), "bar");
            graph.SetEdge("a", "b", new(), "foo");

            Normalize.Run(graph);

            var outEdges = graph.OutEdges("a")!
                .OrderBy(e => e.Name, StringComparer.Ordinal)
                .ToList();
            await Assert.That(outEdges.Count).IsEqualTo(2);

            var barDummy = graph.NodeLabel(outEdges[0].W);
            barDummy.X = 5;
            barDummy.Y = 10;

            var fooDummy = graph.NodeLabel(outEdges[1].W);
            fooDummy.X = 15;
            fooDummy.Y = 20;

            Normalize.Undo(graph);

            await Assert.That(graph.HasEdge("a", "b")).IsFalse();

            var barPoints = graph.FindEdgeLabel("a", "b", "bar").Points!;
            await Assert.That(barPoints.Count).IsEqualTo(1);
            await Assert.That(barPoints[0].X).IsEqualTo(5);
            await Assert.That(barPoints[0].Y).IsEqualTo(10);

            var fooPoints = graph.FindEdgeLabel("a", "b", "foo").Points!;
            await Assert.That(fooPoints.Count).IsEqualTo(1);
            await Assert.That(fooPoints[0].X).IsEqualTo(15);
            await Assert.That(fooPoints[0].Y).IsEqualTo(20);
        }
    }
}
