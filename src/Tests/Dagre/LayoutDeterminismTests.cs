// Regression guard: dagre's output must be independent of the global dummy-node id counter (as real dagre
// is), so a layout is reproducible no matter how many layouts ran before it in the same process.

public class LayoutDeterminismTests
{
    static readonly string[] RealNodes = ["a", "b", "c", "d", "e", "f", "g", "h", "sg1", "sg2"];

    static Graph BuildGraph()
    {
        var graph = new Graph(directed: true, multigraph: true, compound: true);
        graph.SetGraph(
            new()
            {
                Rankdir = "TB",
                Nodesep = 50,
                Ranksep = 50,
                Edgesep = 20
            });
        graph.SetDefaultEdgeLabel(new EdgeLabel());
        foreach (var v in new[] { "a", "b", "c", "d", "e", "f", "g", "h" })
        {
            graph.SetNode(
                v,
                new()
                {
                    Width = 40,
                    Height = 40
                });
        }

        graph.SetNode("sg1", new());
        graph.SetNode("sg2", new());
        graph.SetParent("c", "sg1");
        graph.SetParent("d", "sg1");
        graph.SetParent("f", "sg2");
        graph.SetParent("g", "sg2");

        void E(string a, string b) => graph.SetEdge(a, b, new() { Minlen = 1, Weight = 1 });
        E("a", "b");
        E("b", "c");
        E("b", "d");
        E("c", "e");
        E("d", "e");
        E("a", "f");
        E("f", "g");
        E("g", "h");
        E("e", "h");
        E("a", "h");   // long edge -> dummy chain
        E("b", "g");   // crosses subgraphs -> dummy chain
        E("h", "a");   // back-edge -> cycle (acyclic must reverse), like a "reset" transition
        E("h", "b");   // another back-edge
        E("e", "e");   // self-edge, like a self-transition
        E("h", "h");   // self-edge
        return graph;
    }

    static string Positions(Graph g)
    {
        var builder = new StringBuilder();
        foreach (var v in RealNodes)
        {
            var n = g.NodeLabel(v);
            builder.Append(CultureInfo.InvariantCulture, $"{v}:{n.X:0.##},{n.Y:0.##};");
        }

        return builder.ToString();
    }

    [Test]
    public async Task TwoIndependentLayoutsOfTheSameGraphMatch()
    {
        // The dummy-id counter is per-graph, so a layout's geometry can never depend on how many other
        // graphs were laid out before it. Each fresh graph starts its counter at 0 and is fully reproducible.
        var g1 = BuildGraph();
        Layout.Run(g1);

        var g2 = BuildGraph();
        Layout.Run(g2);

        await Assert.That(Positions(g2)).IsEqualTo(Positions(g1));
    }
}
