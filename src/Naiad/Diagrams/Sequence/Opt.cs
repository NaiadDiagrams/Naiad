namespace MermaidSharp.Diagrams.Sequence;

public class Opt : SequenceElement
{
    public string? Condition { get; set; }
    public List<SequenceElement> Elements { get; } = [];
}