namespace MermaidSharp.Core.Models;

public readonly record struct Position(double X, double Y)
{
    public static Position Zero => new(0, 0);

    public static Position operator +(Position a, Position b) => new(a.X + b.X, a.Y + b.Y);
    public static Position operator -(Position a, Position b) => new(a.X - b.X, a.Y - b.Y);
    public static Position operator *(Position p, double scalar) => new(p.X * scalar, p.Y * scalar);
    public static Position operator /(Position p, double scalar) => new(p.X / scalar, p.Y / scalar);

    public double DistanceTo(Position other)
    {
        var dx = X - other.X;
        var dy = Y - other.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }
}

public readonly record struct Size(double Width, double Height)
{
    public static Size Zero => new(0, 0);
}

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
