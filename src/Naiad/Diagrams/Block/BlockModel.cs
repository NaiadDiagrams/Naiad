namespace MermaidSharp.Diagrams.Block;

public class BlockModel : DiagramBase
{
    public int Columns { get; set; } = 1;
    public List<BlockElement> Elements { get; } = [];
}

public class BlockElement
{
    public required string Id { get; init; }
    public string? Label { get; set; }
    public int Span { get; set; } = 1;
    public BlockShape Shape { get; set; } = BlockShape.Rectangle;
}

public enum BlockShape
{
    Rectangle,    // ["label"]
    Rounded,      // ("label")
    Stadium,      // (["label"])
    Circle,       // (("label"))
    Diamond,      // {"label"}
    Hexagon       // {{"label"}}
}
