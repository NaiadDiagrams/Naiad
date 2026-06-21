using System.Text;

namespace Naiad.Dagre.Tests;

// Regression guard: dagre's output must be independent of the global dummy-node id counter (as real dagre
// is), so a layout is reproducible no matter how many layouts ran before it in the same process.
public class LayoutDeterminismTests
{
    static readonly string[] RealNodes = ["a", "b", "c", "d", "e", "f", "g", "h", "sg1", "sg2"];

    static Graph BuildGraph()
    {
        var g = new Graph(directed: true, multigraph: true, compound: true);
        g.SetGraph(new GraphLabel { Rankdir = "TB", Nodesep = 50, Ranksep = 50, Edgesep = 20 });
        g.SetDefaultEdgeLabel(new EdgeLabel());
        foreach (var v in new[] { "a", "b", "c", "d", "e", "f", "g", "h" })
        {
            g.SetNode(v, new NodeLabel { Width = 40, Height = 40 });
        }

        g.SetNode("sg1", new NodeLabel());
        g.SetNode("sg2", new NodeLabel());
        g.SetParent("c", "sg1");
        g.SetParent("d", "sg1");
        g.SetParent("f", "sg2");
        g.SetParent("g", "sg2");

        void E(string a, string b) => g.SetEdge(a, b, new EdgeLabel { Minlen = 1, Weight = 1 });
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
        return g;
    }

    static string Positions(Graph g)
    {
        var sb = new StringBuilder();
        foreach (var v in RealNodes)
        {
            var n = g.Node(v);
            sb.Append(System.Globalization.CultureInfo.InvariantCulture, $"{v}:{n.X:0.##},{n.Y:0.##};");
        }

        return sb.ToString();
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
