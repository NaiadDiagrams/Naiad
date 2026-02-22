namespace MermaidSharp.Diagrams.Sequence;

public class Activation : SequenceElement
{
    public required string ParticipantId { get; init; }
    public bool IsActivate { get; set; }
}