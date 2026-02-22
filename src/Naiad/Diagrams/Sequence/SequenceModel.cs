namespace MermaidSharp.Diagrams.Sequence;

public class SequenceModel : DiagramBase
{
    public List<Participant> Participants { get; } = [];
    public List<SequenceElement> Elements { get; } = [];
    public bool AutoNumber { get; set; }
}