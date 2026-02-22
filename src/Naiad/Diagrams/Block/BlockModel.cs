namespace MermaidSharp.Diagrams.Block;

public class BlockModel : DiagramBase
{
    public int Columns { get; set; } = 1;
    public List<BlockElement> Elements { get; } = [];
}