public class CoordinateSystemTests
{
    public class AdjustTests
    {
        Graph graph = null!;

        [Before(Test)]
        public void Setup()
        {
            graph = new();
            graph.SetNode(
                "a",
                new()
                {
                    Width = 100,
                    Height = 200
                });
        }

        [Test]
        public async Task DoesNothingToNodeDimensionsWithRankdirTb()
        {
            graph.SetGraph(
                new()
                {
                    Rankdir = Direction.TopToBottom
                });
            CoordinateSystem.Adjust(graph);
            var a = graph.NodeLabel("a");
            await Assert.That(a.Width).IsEqualTo(100);
            await Assert.That(a.Height).IsEqualTo(200);
        }

        [Test]
        public async Task DoesNothingToNodeDimensionsWithRankdirBt()
        {
            graph.SetGraph(new() { Rankdir = Direction.BottomToTop });
            CoordinateSystem.Adjust(graph);
            var a = graph.NodeLabel("a");
            await Assert.That(a.Width).IsEqualTo(100);
            await Assert.That(a.Height).IsEqualTo(200);
        }

        [Test]
        public async Task SwapsWidthAndHeightForNodesWithRankdirLr()
        {
            graph.SetGraph(new() { Rankdir = Direction.LeftToRight });
            CoordinateSystem.Adjust(graph);
            var a = graph.NodeLabel("a");
            await Assert.That(a.Width).IsEqualTo(200);
            await Assert.That(a.Height).IsEqualTo(100);
        }

        [Test]
        public async Task SwapsWidthAndHeightForNodesWithRankdirRl()
        {
            graph.SetGraph(new() { Rankdir = Direction.RightToLeft });
            CoordinateSystem.Adjust(graph);
            var a = graph.NodeLabel("a");
            await Assert.That(a.Width).IsEqualTo(200);
            await Assert.That(a.Height).IsEqualTo(100);
        }
    }

    public class UndoTests
    {
        Graph graph = null!;

        [Before(Test)]
        public void Setup()
        {
            graph = new();
            graph.SetNode("a", new() { Width = 100, Height = 200, X = 20, Y = 40 });
        }

        [Test]
        public async Task DoesNothingToPointsWithRankdirTb()
        {
            graph.SetGraph(new() { Rankdir = Direction.TopToBottom });
            CoordinateSystem.Undo(graph);
            var a = graph.NodeLabel("a");
            await Assert.That(a.X).IsEqualTo(20);
            await Assert.That(a.Y).IsEqualTo(40);
            await Assert.That(a.Width).IsEqualTo(100);
            await Assert.That(a.Height).IsEqualTo(200);
        }

        [Test]
        public async Task FlipsTheYCoordinateForPointsWithRankdirBt()
        {
            graph.SetGraph(new() { Rankdir = Direction.BottomToTop });
            CoordinateSystem.Undo(graph);
            var a = graph.NodeLabel("a");
            await Assert.That(a.X).IsEqualTo(20);
            await Assert.That(a.Y).IsEqualTo(-40);
            await Assert.That(a.Width).IsEqualTo(100);
            await Assert.That(a.Height).IsEqualTo(200);
        }

        [Test]
        public async Task SwapsDimensionsAndCoordinatesForPointsWithRankdirLr()
        {
            graph.SetGraph(new() { Rankdir = Direction.LeftToRight });
            CoordinateSystem.Undo(graph);
            var a = graph.NodeLabel("a");
            await Assert.That(a.X).IsEqualTo(40);
            await Assert.That(a.Y).IsEqualTo(20);
            await Assert.That(a.Width).IsEqualTo(200);
            await Assert.That(a.Height).IsEqualTo(100);
        }

        [Test]
        public async Task SwapsDimsAndCoordsAndFlipsXForPointsWithRankdirRl()
        {
            graph.SetGraph(new() { Rankdir = Direction.RightToLeft });
            CoordinateSystem.Undo(graph);
            var a = graph.NodeLabel("a");
            await Assert.That(a.X).IsEqualTo(-40);
            await Assert.That(a.Y).IsEqualTo(20);
            await Assert.That(a.Width).IsEqualTo(200);
            await Assert.That(a.Height).IsEqualTo(100);
        }
    }
}
