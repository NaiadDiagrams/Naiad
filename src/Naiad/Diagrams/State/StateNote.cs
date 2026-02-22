    namespace MermaidSharp.Diagrams.State;

public class StateNote
{
    public required string Text { get; init; }
    public required string StateId { get; init; }
    public NotePosition Position { get; set; } = NotePosition.RightOf;
}