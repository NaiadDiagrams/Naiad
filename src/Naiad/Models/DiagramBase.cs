namespace Naiad.Models;

public abstract class DiagramBase
{
    public string? Title { get; set; }
    public Direction Direction { get; set; } = Direction.TopToBottom;
}
