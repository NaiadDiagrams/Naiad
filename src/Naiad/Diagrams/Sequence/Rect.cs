namespace MermaidSharp.Diagrams.Sequence;

public class Rect : SequenceElement
{
    public string? Color { get; set; }
    public List<SequenceElement> Elements { get; } = [];
}