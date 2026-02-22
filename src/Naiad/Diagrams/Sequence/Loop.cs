namespace MermaidSharp.Diagrams.Sequence;

public class Loop : SequenceElement
{
    public string? Label { get; set; }
    public List<SequenceElement> Elements { get; } = [];
}