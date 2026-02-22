namespace MermaidSharp.Models;

public abstract class GraphDiagramBase : DiagramBase
{
    public List<Node> Nodes { get; } = [];
    public List<Edge> Edges { get; } = [];
    public List<Subgraph> Subgraphs { get; } = [];

    public Node? GetNode(string id) => Nodes.Find(_ => _.Id == id);

    public void AddNode(Node node)
    {
        if (Nodes.All(_ => _.Id != node.Id))
        {
            Nodes.Add(node);
        }
    }

    public void AddEdge(Edge edge) =>
        Edges.Add(edge);
}