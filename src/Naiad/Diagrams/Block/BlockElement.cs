namespace Naiad.Diagrams.Block;

public class BlockElement
{
    public required string Id { get; init; }
    public required string? Label { get; init; }
    public required int Span { get; init; }
    public required BlockShape Shape { get; init; }
}