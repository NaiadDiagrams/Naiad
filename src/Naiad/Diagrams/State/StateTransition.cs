namespace Naiad.Diagrams.State;

public class StateTransition
{
    public required string FromId { get; init; }
    public required string ToId { get; init; }
    public string? Label { get; set; }
}
