namespace Naiad.Models;

public readonly record struct Position(double X, double Y)
{
    public static Position Zero => new(0, 0);

    public static Position operator +(Position a, Position b) => new(a.X + b.X, a.Y + b.Y);
    public static Position operator -(Position a, Position b) => new(a.X - b.X, a.Y - b.Y);
    public static Position operator *(Position p, double scalar) => new(p.X * scalar, p.Y * scalar);
    public static Position operator /(Position p, double scalar) => new(p.X / scalar, p.Y / scalar);
}