namespace MermaidSharp.Diagrams.State;

public class StateTransition
{
    public required string FromId { get; init; }
    public required string ToId { get; init; }
    public string? Label { get; set; }

    // Layout properties
    public List<Position> Points { get; } = [];
}