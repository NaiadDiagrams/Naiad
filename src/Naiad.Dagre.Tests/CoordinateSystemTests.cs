namespace Naiad.Dagre.Tests;

// Port of dagre's test/coordinate-system-test.ts.
public class CoordinateSystemTests
{
    Graph g = null!;

    [Before(Test)]
    public void Setup() => g = new Graph();

    public class AdjustTests
    {
        Graph g = null!;

        [Before(Test)]
        public void Setup()
        {
            g = new Graph();
            g.SetNode("a", new NodeLabel { Width = 100, Height = 200 });
        }

        [Test]
        public async Task DoesNothingToNodeDimensionsWithRankdirTb()
        {
            g.SetGraph(new GraphLabel { Rankdir = "TB" });
            CoordinateSystem.Adjust(g);
            var a = g.Node("a");
            await Assert.That(a.Width).IsEqualTo(100);
            await Assert.That(a.Height).IsEqualTo(200);
        }

        [Test]
        public async Task DoesNothingToNodeDimensionsWithRankdirBt()
        {
            g.SetGraph(new GraphLabel { Rankdir = "BT" });
            CoordinateSystem.Adjust(g);
            var a = g.Node("a");
            await Assert.That(a.Width).IsEqualTo(100);
            await Assert.That(a.Height).IsEqualTo(200);
        }

        [Test]
        public async Task SwapsWidthAndHeightForNodesWithRankdirLr()
        {
            g.SetGraph(new GraphLabel { Rankdir = "LR" });
            CoordinateSystem.Adjust(g);
            var a = g.Node("a");
            await Assert.That(a.Width).IsEqualTo(200);
            await Assert.That(a.Height).IsEqualTo(100);
        }

        [Test]
        public async Task SwapsWidthAndHeightForNodesWithRankdirRl()
        {
            g.SetGraph(new GraphLabel { Rankdir = "RL" });
            CoordinateSystem.Adjust(g);
            var a = g.Node("a");
            await Assert.That(a.Width).IsEqualTo(200);
            await Assert.That(a.Height).IsEqualTo(100);
        }
    }

    public class UndoTests
    {
        Graph g = null!;

        [Before(Test)]
        public void Setup()
        {
            g = new Graph();
            g.SetNode("a", new NodeLabel { Width = 100, Height = 200, X = 20, Y = 40 });
        }

        [Test]
        public async Task DoesNothingToPointsWithRankdirTb()
        {
            g.SetGraph(new GraphLabel { Rankdir = "TB" });
            CoordinateSystem.Undo(g);
            var a = g.Node("a");
            await Assert.That(a.X).IsEqualTo(20);
            await Assert.That(a.Y).IsEqualTo(40);
            await Assert.That(a.Width).IsEqualTo(100);
            await Assert.That(a.Height).IsEqualTo(200);
        }

        [Test]
        public async Task FlipsTheYCoordinateForPointsWithRankdirBt()
        {
            g.SetGraph(new GraphLabel { Rankdir = "BT" });
            CoordinateSystem.Undo(g);
            var a = g.Node("a");
            await Assert.That(a.X).IsEqualTo(20);
            await Assert.That(a.Y).IsEqualTo(-40);
            await Assert.That(a.Width).IsEqualTo(100);
            await Assert.That(a.Height).IsEqualTo(200);
        }

        [Test]
        public async Task SwapsDimensionsAndCoordinatesForPointsWithRankdirLr()
        {
            g.SetGraph(new GraphLabel { Rankdir = "LR" });
            CoordinateSystem.Undo(g);
            var a = g.Node("a");
            await Assert.That(a.X).IsEqualTo(40);
            await Assert.That(a.Y).IsEqualTo(20);
            await Assert.That(a.Width).IsEqualTo(200);
            await Assert.That(a.Height).IsEqualTo(100);
        }

        [Test]
        public async Task SwapsDimsAndCoordsAndFlipsXForPointsWithRankdirRl()
        {
            g.SetGraph(new GraphLabel { Rankdir = "RL" });
            CoordinateSystem.Undo(g);
            var a = g.Node("a");
            await Assert.That(a.X).IsEqualTo(-40);
            await Assert.That(a.Y).IsEqualTo(20);
            await Assert.That(a.Width).IsEqualTo(200);
            await Assert.That(a.Height).IsEqualTo(100);
        }
    }
}
