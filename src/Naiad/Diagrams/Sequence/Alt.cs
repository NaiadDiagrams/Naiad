namespace MermaidSharp.Diagrams.Sequence;

public class Alt : SequenceElement
{
    public string? Condition { get; set; }
    public List<SequenceElement> Elements { get; } = [];
    public List<AltElse> ElseBranches { get; } = [];
}