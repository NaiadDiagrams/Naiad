public class GraphTests
{
    public class FindEdgeLabelTests
    {
        [Test]
        public async Task ThrowsWhenTheEdgeDoesNotExist()
        {
            var graph = new Graph();
            graph.SetNode("a");
            graph.SetNode("b");

            await Assert.That(() => graph.FindEdgeLabel("a", "b")).Throws<KeyNotFoundException>();
        }

        [Test]
        public async Task ThrowsForTheEdgeOverloadWhenTheEdgeDoesNotExist()
        {
            var graph = new Graph();

            await Assert.That(() => graph.FindEdgeLabel(new Edge("a", "b"))).Throws<KeyNotFoundException>();
        }

        [Test]
        public async Task ReturnsTheLabelWhenTheEdgeExists()
        {
            var graph = new Graph();
            var label = new EdgeLabel { Weight = 2 };
            graph.SetEdge("a", "b", label);

            await Assert.That(graph.FindEdgeLabel("a", "b")).IsSameReferenceAs(label);
        }
    }

    public class TryGetEdgeLabelTests
    {
        [Test]
        public async Task ReturnsFalseWhenTheEdgeDoesNotExist()
        {
            var graph = new Graph();
            graph.SetNode("a");
            graph.SetNode("b");

            await Assert.That(graph.TryGetEdgeLabel("a", "b", out _)).IsFalse();
        }

        [Test]
        public async Task ReturnsTrueWithTheLabelWhenTheEdgeExists()
        {
            var graph = new Graph();
            var label = new EdgeLabel { Weight = 2 };
            graph.SetEdge("a", "b", label);

            await Assert.That(graph.TryGetEdgeLabel("a", "b", out var found)).IsTrue();
            await Assert.That(found).IsSameReferenceAs(label);
        }

        [Test]
        public async Task EdgeOverloadProbesExistence()
        {
            var graph = new Graph();
            graph.SetEdge("a", "b", new EdgeLabel { Weight = 1 });

            await Assert.That(graph.TryGetEdgeLabel(new Edge("a", "b"), out _)).IsTrue();
            await Assert.That(graph.TryGetEdgeLabel(new Edge("b", "c"), out _)).IsFalse();
        }
    }

    public class NodeLabelTests
    {
        [Test]
        public async Task ThrowsWhenTheNodeDoesNotExist()
        {
            var graph = new Graph();

            await Assert.That(() => graph.NodeLabel("a")).Throws<KeyNotFoundException>();
        }

        [Test]
        public async Task ThrowsWhenTheNodeExistsButHasNoLabel()
        {
            var graph = new Graph();
            graph.SetNode("a"); // no label → the default node-label fn yields null

            await Assert.That(graph.HasNode("a")).IsTrue();
            await Assert.That(() => graph.NodeLabel("a")).Throws<KeyNotFoundException>();
        }

        [Test]
        public async Task ReturnsTheLabelWhenTheNodeHasOne()
        {
            var graph = new Graph();
            var label = new NodeLabel { Width = 2 };
            graph.SetNode("a", label);

            await Assert.That(graph.NodeLabel("a")).IsSameReferenceAs(label);
        }
    }

    public class TryGetNodeLabelTests
    {
        [Test]
        public async Task ReturnsFalseWhenTheNodeDoesNotExist()
        {
            var graph = new Graph();

            await Assert.That(graph.TryGetNodeLabel("a", out _)).IsFalse();
        }

        [Test]
        public async Task ReturnsFalseWhenTheNodeExistsButHasNoLabel()
        {
            var graph = new Graph();
            graph.SetNode("a");

            await Assert.That(graph.TryGetNodeLabel("a", out _)).IsFalse();
        }

        [Test]
        public async Task ReturnsTrueWithTheLabelWhenTheNodeHasOne()
        {
            var graph = new Graph();
            var label = new NodeLabel { Width = 2 };
            graph.SetNode("a", label);

            await Assert.That(graph.TryGetNodeLabel("a", out var found)).IsTrue();
            await Assert.That(found).IsSameReferenceAs(label);
        }
    }
}
