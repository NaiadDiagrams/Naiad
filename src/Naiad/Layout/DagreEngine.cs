using DagreGraph = Naiad.Dagre.Graph;
using DagreNodeLabel = Naiad.Dagre.NodeLabel;
using DagreEdgeLabel = Naiad.Dagre.EdgeLabel;

/// <summary>
/// Lays out a node/edge diagram with the faithful C# port of dagre (<c>Naiad.Dagre</c>) — the same
/// layered/Sugiyama engine Mermaid uses. The diagram is mapped to a compound dagre graph (subgraphs become
/// compound parent nodes), laid out, and the resulting node positions, cluster boxes and routed edge points
/// are read back. This gives Mermaid-equivalent ranking, crossing minimisation, cluster nesting and
/// shape-avoiding edge routing.
/// </summary>
/// <remarks>
/// Read-back relies on a contract of <see cref="Naiad.Dagre.Layout.Run"/>: it writes the final layout
/// (positions, cluster sizes, routed points) back onto the very label instances passed in here via
/// <c>UpdateInputGraph</c>, rather than returning fresh labels. So we keep a reference to each label we
/// hand to the graph and read results straight off it — no second keyed lookup. If a future re-sync with
/// upstream dagre changes that to return new labels, this read-back must switch back to per-id lookups.
/// </remarks>
class DagreEngine : ILayoutEngine
{
    public LayoutResult Layout(GraphDiagramBase diagram, LayoutOptions options)
    {
        if (diagram.Nodes.Count == 0)
        {
            return new() { Width = 0, Height = 0 };
        }

        var graph = new DagreGraph(directed: true, multigraph: true, compound: true);
        graph.SetGraph(new()
        {
            Rankdir = options.Direction,
            NodeSeparation = options.NodeSeparation,
            RankSeparation = options.RankSeparation
        });
        graph.SetDefaultEdgeLabel(new DagreEdgeLabel());

        var nodeLabels = new List<(Node Node, DagreNodeLabel Label)>(diagram.Nodes.Count);
        foreach (var node in diagram.Nodes)
        {
            var label = new DagreNodeLabel { Width = node.Width, Height = node.Height };
            graph.SetNode(node.Id, label);
            nodeLabels.Add((node, label));
        }

        var subgraphLabels = new List<(Subgraph Subgraph, DagreNodeLabel Label)>();
        foreach (var subgraph in diagram.Subgraphs)
        {
            AddSubgraph(graph, subgraph, subgraphLabels);
        }

        var edgeLabels = new List<(Edge Edge, DagreEdgeLabel Label)>(diagram.Edges.Count);
        for (var i = 0; i < diagram.Edges.Count; i++)
        {
            var edge = diagram.Edges[i];
            var label = new DagreEdgeLabel();
            if (!string.IsNullOrEmpty(edge.Label))
            {
                label.Width = edge.LabelWidth;
                label.Height = edge.LabelHeight;
                label.Labelpos = Naiad.Dagre.LabelPos.Center;
            }

            // A unique per-edge name keeps parallel edges (same source/target) distinct in the multigraph.
            graph.SetEdge(edge.SourceId, edge.TargetId, label, "e" + i.ToString(CultureInfo.InvariantCulture));
            edgeLabels.Add((edge, label));
        }

        Naiad.Dagre.Layout.Run(graph);

        foreach (var (node, label) in nodeLabels)
        {
            node.Position = new(label.X ?? 0, label.Y ?? 0);
        }

        foreach (var (subgraph, label) in subgraphLabels)
        {
            subgraph.Position = new(label.X ?? 0, label.Y ?? 0);
            subgraph.Width = label.Width;
            subgraph.Height = label.Height;
        }

        foreach (var (edge, label) in edgeLabels)
        {
            edge.Points.Clear();
            if (label.Points != null)
            {
                edge.Points.AddRange(label.Points);
            }
        }

        var graphLabel = graph.Label;
        return new()
        {
            Width = graphLabel.Width ?? 0,
            Height = graphLabel.Height ?? 0
        };
    }

    static void AddSubgraph(DagreGraph graph, Subgraph subgraph, List<(Subgraph, DagreNodeLabel)> collected)
    {
        var label = new DagreNodeLabel();
        graph.SetNode(subgraph.Id, label);
        collected.Add((subgraph, label));

        foreach (var nodeId in subgraph.NodeIds)
        {
            graph.SetParent(nodeId, subgraph.Id);
        }

        foreach (var nested in subgraph.NestedSubgraphs)
        {
            AddSubgraph(graph, nested, collected);
            graph.SetParent(nested.Id, subgraph.Id);
        }
    }
}
