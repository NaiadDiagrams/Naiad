namespace MermaidSharp.Models;

public readonly record struct Rect(double X, double Y, double Width, double Height)
{
    public double Left => X;
    public double Right => X + Width;
    public double Top => Y;
    public double Bottom => Y + Height;
    public Position Center => new(X + Width / 2, Y + Height / 2);

    public bool Contains(Position p) =>
        p.X >= Left && p.X <= Right && p.Y >= Top && p.Y <= Bottom;

    public bool Intersects(Rect other) =>
        Left < other.Right && Right > other.Left &&
        Top < other.Bottom && Bottom > other.Top;
}