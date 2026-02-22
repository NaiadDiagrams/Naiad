namespace MermaidSharp.Diagrams.Sequence;

public class Note : SequenceElement
{
    public required string Text { get; init; }
    public NotePosition Position { get; set; } = NotePosition.RightOf;
    public required string ParticipantId { get; init; }
    public string? OverParticipantId2 { get; set; }
}