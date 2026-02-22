namespace MermaidSharp.Diagrams.Sequence;

public class Break : SequenceElement
{
    public string? Label { get; set; }
    public List<SequenceElement> Elements { get; } = [];
}