namespace MermaidSharp.Rendering;

public class SvgFilter
{
    public required string Id { get; init; }
    public required string Content { get; init; }

    public void ToXml(StringBuilder builder) => builder.Append($"<filter id=\"{Id}\">{Content}</filter>");
}