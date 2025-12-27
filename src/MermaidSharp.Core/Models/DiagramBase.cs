namespace MermaidSharp.Core.Models;

public abstract class DiagramBase
{
    public string? Title { get; set; }
    public string? AccessibilityTitle { get; set; }
    public string? AccessibilityDescription { get; set; }
    public Direction Direction { get; set; } = Direction.TopToBottom;
    public Dictionary<string, string> Config { get; } = new();
}

public abstract class GraphDiagramBase : DiagramBase
{
    public List<Node> Nodes { get; } = [];
    public List<Edge> Edges { get; } = [];
    public List<Subgraph> Subgraphs { get; } = [];

    public Node? GetNode(string id) => Nodes.Find(n => n.Id == id);

    public void AddNode(Node node)
    {
        if (Nodes.All(n => n.Id != node.Id))
        {
            Nodes.Add(node);
        }
    }

    public void AddEdge(Edge edge)
    {
        Edges.Add(edge);
    }
}

public class Subgraph
{
    public required string Id { get; init; }
    public string? Title { get; set; }
    public Direction Direction { get; set; } = Direction.TopToBottom;
    public List<string> NodeIds { get; } = [];
    public List<Subgraph> NestedSubgraphs { get; } = [];

    // Layout properties
    public Position Position { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }

    public Rect Bounds => new(
        Position.X - Width / 2,
        Position.Y - Height / 2,
        Width,
        Height);
}
