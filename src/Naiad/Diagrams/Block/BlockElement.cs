namespace MermaidSharp.Diagrams.Block;

public class BlockElement
{
    public required string Id { get; init; }
    public string? Label { get; set; }
    public int Span { get; set; } = 1;
    public BlockShape Shape { get; set; } = BlockShape.Rectangle;
}