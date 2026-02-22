namespace MermaidSharp.Models;

public readonly record struct Size(double Width, double Height)
{
    public static Size Zero => new(0, 0);
}