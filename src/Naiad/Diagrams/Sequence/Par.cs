namespace MermaidSharp.Diagrams.Sequence;

public class Par : SequenceElement
{
    public string? Label { get; set; }
    public List<SequenceElement> Elements { get; } = [];
    public List<ParAnd> AndBranches { get; } = [];
}