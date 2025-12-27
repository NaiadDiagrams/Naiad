namespace MermaidSharp.Models;

public abstract class DiagramBase
{
    public string? Title { get; set; }
    public string? AccessibilityTitle { get; set; }
    public string? AccessibilityDescription { get; set; }
    public Direction Direction { get; set; } = Direction.TopToBottom;
    public Dictionary<string, string> Config { get; } = new();
}