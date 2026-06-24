namespace Naiad;

public abstract class GraphDiagramBase : DiagramBase
{
    // Index alongside Nodes for O(1) GetNode/AddNode dedup (both were O(n) linear scans, making graph
    // construction and per-edge lookups quadratic). All node additions must go through AddNode to keep
    // this in sync — Nodes itself is only ever appended to via AddNode.
    Dictionary<string, Node> nodesById = new(StringComparer.Ordinal);

    public List<Node> Nodes { get; } = [];
    public List<Edge> Edges { get; } = [];
    public List<Subgraph> Subgraphs { get; } = [];

    public Node? GetNode(string id) => nodesById.GetValueOrDefault(id);

    public void AddNode(Node node)
    {
        if (nodesById.TryAdd(node.Id, node))
        {
            Nodes.Add(node);
        }
    }

    public void AddEdge(Edge edge) =>
        Edges.Add(edge);
}