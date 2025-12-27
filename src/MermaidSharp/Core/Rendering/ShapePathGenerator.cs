namespace MermaidSharp;

public static class ShapePathGenerator
{
    public static string Rectangle(double x, double y, double width, double height, double rx = 0)
    {
        if (rx > 0)
        {
            return $"M{Fmt(x + rx)},{Fmt(y)} " +
                   $"H{Fmt(x + width - rx)} Q{Fmt(x + width)},{Fmt(y)} {Fmt(x + width)},{Fmt(y + rx)} " +
                   $"V{Fmt(y + height - rx)} Q{Fmt(x + width)},{Fmt(y + height)} {Fmt(x + width - rx)},{Fmt(y + height)} " +
                   $"H{Fmt(x + rx)} Q{Fmt(x)},{Fmt(y + height)} {Fmt(x)},{Fmt(y + height - rx)} " +
                   $"V{Fmt(y + rx)} Q{Fmt(x)},{Fmt(y)} {Fmt(x + rx)},{Fmt(y)} Z";
        }
        return $"M{Fmt(x)},{Fmt(y)} H{Fmt(x + width)} V{Fmt(y + height)} H{Fmt(x)} Z";
    }

    public static string Circle(double cx, double cy, double r)
    {
        return $"M{Fmt(cx)},{Fmt(cy - r)} " +
               $"A{Fmt(r)},{Fmt(r)} 0 1 1 {Fmt(cx)},{Fmt(cy + r)} " +
               $"A{Fmt(r)},{Fmt(r)} 0 1 1 {Fmt(cx)},{Fmt(cy - r)} Z";
    }

    public static string Ellipse(double cx, double cy, double rx, double ry)
    {
        return $"M{Fmt(cx)},{Fmt(cy - ry)} " +
               $"A{Fmt(rx)},{Fmt(ry)} 0 1 1 {Fmt(cx)},{Fmt(cy + ry)} " +
               $"A{Fmt(rx)},{Fmt(ry)} 0 1 1 {Fmt(cx)},{Fmt(cy - ry)} Z";
    }

    public static string Diamond(double cx, double cy, double width, double height)
    {
        var w2 = width / 2;
        var h2 = height / 2;
        return $"M{Fmt(cx)},{Fmt(cy - h2)} L{Fmt(cx + w2)},{Fmt(cy)} L{Fmt(cx)},{Fmt(cy + h2)} L{Fmt(cx - w2)},{Fmt(cy)} Z";
    }

    public static string Hexagon(double cx, double cy, double width, double height)
    {
        var w4 = width / 4;
        var w2 = width / 2;
        var h2 = height / 2;
        return $"M{Fmt(cx - w4)},{Fmt(cy - h2)} " +
               $"L{Fmt(cx + w4)},{Fmt(cy - h2)} " +
               $"L{Fmt(cx + w2)},{Fmt(cy)} " +
               $"L{Fmt(cx + w4)},{Fmt(cy + h2)} " +
               $"L{Fmt(cx - w4)},{Fmt(cy + h2)} " +
               $"L{Fmt(cx - w2)},{Fmt(cy)} Z";
    }

    public static string Stadium(double x, double y, double width, double height)
    {
        var r = height / 2;
        return $"M{Fmt(x + r)},{Fmt(y)} " +
               $"H{Fmt(x + width - r)} " +
               $"A{Fmt(r)},{Fmt(r)} 0 0 1 {Fmt(x + width - r)},{Fmt(y + height)} " +
               $"H{Fmt(x + r)} " +
               $"A{Fmt(r)},{Fmt(r)} 0 0 1 {Fmt(x + r)},{Fmt(y)} Z";
    }

    public static string Cylinder(double x, double y, double width, double height)
    {
        var ry = height * 0.1;
        var bodyHeight = height - ry * 2;
        return $"M{Fmt(x)},{Fmt(y + ry)} " +
               $"A{Fmt(width / 2)},{Fmt(ry)} 0 0 1 {Fmt(x + width)},{Fmt(y + ry)} " +
               $"V{Fmt(y + ry + bodyHeight)} " +
               $"A{Fmt(width / 2)},{Fmt(ry)} 0 0 1 {Fmt(x)},{Fmt(y + ry + bodyHeight)} " +
               $"V{Fmt(y + ry)} Z " +
               $"M{Fmt(x)},{Fmt(y + ry)} " +
               $"A{Fmt(width / 2)},{Fmt(ry)} 0 0 0 {Fmt(x + width)},{Fmt(y + ry)}";
    }

    public static string Parallelogram(double x, double y, double width, double height, double skew = 0.2)
    {
        var offset = width * skew;
        return $"M{Fmt(x + offset)},{Fmt(y)} " +
               $"L{Fmt(x + width)},{Fmt(y)} " +
               $"L{Fmt(x + width - offset)},{Fmt(y + height)} " +
               $"L{Fmt(x)},{Fmt(y + height)} Z";
    }

    public static string ParallelogramAlt(double x, double y, double width, double height, double skew = 0.2)
    {
        var offset = width * skew;
        return $"M{Fmt(x)},{Fmt(y)} " +
               $"L{Fmt(x + width - offset)},{Fmt(y)} " +
               $"L{Fmt(x + width)},{Fmt(y + height)} " +
               $"L{Fmt(x + offset)},{Fmt(y + height)} Z";
    }

    public static string Trapezoid(double x, double y, double width, double height, double skew = 0.2)
    {
        var offset = width * skew;
        return $"M{Fmt(x + offset)},{Fmt(y)} " +
               $"L{Fmt(x + width - offset)},{Fmt(y)} " +
               $"L{Fmt(x + width)},{Fmt(y + height)} " +
               $"L{Fmt(x)},{Fmt(y + height)} Z";
    }

    public static string TrapezoidAlt(double x, double y, double width, double height, double skew = 0.2)
    {
        var offset = width * skew;
        return $"M{Fmt(x)},{Fmt(y)} " +
               $"L{Fmt(x + width)},{Fmt(y)} " +
               $"L{Fmt(x + width - offset)},{Fmt(y + height)} " +
               $"L{Fmt(x + offset)},{Fmt(y + height)} Z";
    }

    public static string Asymmetric(double x, double y, double width, double height)
    {
        var notch = width * 0.15;
        return $"M{Fmt(x + notch)},{Fmt(y)} " +
               $"L{Fmt(x + width)},{Fmt(y)} " +
               $"L{Fmt(x + width)},{Fmt(y + height)} " +
               $"L{Fmt(x + notch)},{Fmt(y + height)} " +
               $"L{Fmt(x)},{Fmt(y + height / 2)} Z";
    }

    public static string Subroutine(double x, double y, double width, double height)
    {
        var inset = width * 0.1;
        return $"M{Fmt(x)},{Fmt(y)} H{Fmt(x + width)} V{Fmt(y + height)} H{Fmt(x)} Z " +
               $"M{Fmt(x + inset)},{Fmt(y)} V{Fmt(y + height)} " +
               $"M{Fmt(x + width - inset)},{Fmt(y)} V{Fmt(y + height)}";
    }

    public static string DoubleCircle(double cx, double cy, double r)
    {
        var innerR = r * 0.85;
        return Circle(cx, cy, r) + " " + Circle(cx, cy, innerR);
    }

    public static string Document(double x, double y, double width, double height)
    {
        var waveHeight = height * 0.15;
        var bodyHeight = height - waveHeight;
        return $"M{Fmt(x)},{Fmt(y)} " +
               $"H{Fmt(x + width)} " +
               $"V{Fmt(y + bodyHeight)} " +
               $"Q{Fmt(x + width * 0.75)},{Fmt(y + height + waveHeight * 0.5)} " +
               $"{Fmt(x + width * 0.5)},{Fmt(y + bodyHeight)} " +
               $"Q{Fmt(x + width * 0.25)},{Fmt(y + bodyHeight - waveHeight * 0.5)} " +
               $"{Fmt(x)},{Fmt(y + bodyHeight)} Z";
    }

    public static string GetPath(NodeShape shape, double x, double y, double width, double height)
    {
        var cx = x + width / 2;
        var cy = y + height / 2;
        var r = Math.Min(width, height) / 2;

        return shape switch
        {
            NodeShape.Rectangle => Rectangle(x, y, width, height),
            NodeShape.RoundedRectangle => Rectangle(x, y, width, height, 5),
            NodeShape.Circle => Circle(cx, cy, r),
            NodeShape.DoubleCircle => DoubleCircle(cx, cy, r),
            NodeShape.Diamond => Diamond(cx, cy, width, height),
            NodeShape.Hexagon => Hexagon(cx, cy, width, height),
            NodeShape.Stadium => Stadium(x, y, width, height),
            NodeShape.Cylinder => Cylinder(x, y, width, height),
            NodeShape.Parallelogram => Parallelogram(x, y, width, height),
            NodeShape.ParallelogramAlt => ParallelogramAlt(x, y, width, height),
            NodeShape.Trapezoid => Trapezoid(x, y, width, height),
            NodeShape.TrapezoidAlt => TrapezoidAlt(x, y, width, height),
            NodeShape.Asymmetric => Asymmetric(x, y, width, height),
            NodeShape.Subroutine => Subroutine(x, y, width, height),
            NodeShape.Document => Document(x, y, width, height),
            _ => Rectangle(x, y, width, height)
        };
    }

    static string Fmt(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
}
