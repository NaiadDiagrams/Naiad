using DagreGraph = Naiad.Dagre.Graph;
using DagreEdgeLabel = Naiad.Dagre.EdgeLabel;

/// <summary>
/// Lays out a node/edge diagram with the faithful C# port of dagre (<c>Naiad.Dagre</c>) — the same
/// layered/Sugiyama engine Mermaid uses. The diagram is mapped to a compound dagre graph (subgraphs become
/// compound parent nodes), laid out, and the resulting node positions, cluster boxes and routed edge points
/// are read back. This gives Mermaid-equivalent ranking, crossing minimisation, cluster nesting and
/// shape-avoiding edge routing.
/// </summary>
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
            Nodesep = options.NodeSeparation,
            Ranksep = options.RankSeparation
        });
        graph.SetDefaultEdgeLabel(new DagreEdgeLabel());

        foreach (var node in diagram.Nodes)
        {
            graph.SetNode(node.Id, new() { Width = node.Width, Height = node.Height });
        }

        foreach (var subgraph in diagram.Subgraphs)
        {
            AddSubgraph(graph, subgraph);
        }

        var edgeNames = new List<string>(diagram.Edges.Count);
        for (var i = 0; i < diagram.Edges.Count; i++)
        {
            var edge = diagram.Edges[i];
            var name = "e" + i.ToString(CultureInfo.InvariantCulture);
            edgeNames.Add(name);

            var label = new DagreEdgeLabel
            {
                Minlen = 1,
                Weight = 1
            };
            if (!string.IsNullOrEmpty(edge.Label))
            {
                label.Width = edge.LabelWidth;
                label.Height = edge.LabelHeight;
                label.Labelpos = Naiad.Dagre.LabelPos.Center;
            }

            graph.SetEdge(edge.SourceId, edge.TargetId, label, name);
        }

        Naiad.Dagre.Layout.Run(graph);

        foreach (var node in diagram.Nodes)
        {
            var laidOut = graph.NodeLabel(node.Id);
            node.Position = new(laidOut.X ?? 0, laidOut.Y ?? 0);
        }

        foreach (var subgraph in Flatten(diagram.Subgraphs))
        {
            var laidOut = graph.NodeLabel(subgraph.Id);
            subgraph.Position = new(laidOut.X ?? 0, laidOut.Y ?? 0);
            subgraph.Width = laidOut.Width;
            subgraph.Height = laidOut.Height;
        }

        for (var i = 0; i < diagram.Edges.Count; i++)
        {
            var edge = diagram.Edges[i];
            var label = graph.FindEdgeLabel(edge.SourceId, edge.TargetId, edgeNames[i]);
            edge.Points.Clear();
            if (label.Points != null)
            {
                foreach (var point in label.Points)
                {
                    edge.Points.Add(new(point.X, point.Y));
                }
            }
        }

        var graphLabel = graph.Label;
        return new()
        {
            Width = graphLabel.Width ?? 0,
            Height = graphLabel.Height ?? 0
        };
    }

    static void AddSubgraph(DagreGraph graph, Subgraph subgraph)
    {
        graph.SetNode(subgraph.Id, new());
        foreach (var nodeId in subgraph.NodeIds)
        {
            graph.SetParent(nodeId, subgraph.Id);
        }

        foreach (var nested in subgraph.NestedSubgraphs)
        {
            AddSubgraph(graph, nested);
            graph.SetParent(nested.Id, subgraph.Id);
        }
    }

    static IEnumerable<Subgraph> Flatten(IEnumerable<Subgraph> subgraphs)
    {
        foreach (var subgraph in subgraphs)
        {
            yield return subgraph;
            foreach (var nested in Flatten(subgraph.NestedSubgraphs))
            {
                yield return nested;
            }
        }
    }
}
