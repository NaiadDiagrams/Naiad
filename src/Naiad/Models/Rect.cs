namespace Naiad.Models;

public readonly record struct Rect(double X, double Y, double Width, double Height)
{
    public double Left => X;
    public double Right => X + Width;
    public double Top => Y;
    public double Bottom => Y + Height;
}
