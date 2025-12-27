namespace MermaidSharp.Rendering;

public class SvgFilter
{
    public required string Id { get; init; }
    public required string Content { get; init; }

    public string ToXml() => $"<filter id=\"{Id}\">{Content}</filter>";
}