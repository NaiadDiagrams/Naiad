namespace MermaidSharp.Diagrams.Treemap;

public class TreemapNode
{
    public required string Name { get; init; }
    public double? Value { get; set; }
    public List<TreemapNode> Children { get; } = [];
    public string? CssClass { get; set; }

    public bool IsLeaf => Children.Count == 0;

    public double TotalValue => IsLeaf
        ? Value ?? 0
        : Children.Sum(c => c.TotalValue);
}