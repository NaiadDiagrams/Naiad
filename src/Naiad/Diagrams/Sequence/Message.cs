namespace MermaidSharp.Diagrams.Sequence;

public class Message : SequenceElement
{
    public required string FromId { get; init; }
    public required string ToId { get; init; }
    public string? Text { get; set; }
    public MessageType Type { get; set; } = MessageType.Solid;
    public bool Activate { get; set; }
    public bool Deactivate { get; set; }
}