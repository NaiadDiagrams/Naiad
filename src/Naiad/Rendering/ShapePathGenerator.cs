namespace MermaidSharp.Rendering;

public static class ShapePathGenerator
{
    static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    static string Rectangle(double x, double y, double width, double height, double rx = 0)
    {
        if (rx > 0)
        {
            return string.Create(
                Inv,
                $"""
                 M{x + rx:0.##},{y:0.##}
                 H{x + width - rx:0.##}
                 Q{x + width:0.##},{y:0.##} {x + width:0.##},{y + rx:0.##}
                 V{y + height - rx:0.##}
                 Q{x + width:0.##},{y + height:0.##} {x + width - rx:0.##},{y + height:0.##}
                 H{x + rx:0.##}
                 Q{x:0.##},{y + height:0.##} {x:0.##},{y + height - rx:0.##}
                 V{y + rx:0.##}
                 Q{x:0.##},{y:0.##} {x + rx:0.##},{y:0.##} Z
                 """);
        }

        return string.Create(Inv, $"M{x:0.##},{y:0.##} H{x + width:0.##} V{y + height:0.##} H{x:0.##} Z");
    }

    static string Circle(double cx, double cy, double r) =>
        string.Create(
            Inv,
            $"""
             M{cx:0.##},{cy - r:0.##}
             A{r:0.##},{r:0.##} 0 1 1 {cx:0.##},{cy + r:0.##}
             A{r:0.##},{r:0.##} 0 1 1 {cx:0.##},{cy - r:0.##} Z
             """);

    public static string Ellipse(double cx, double cy, double rx, double ry) =>
        string.Create(
            Inv,
            $"""
             M{cx:0.##},{cy - ry:0.##}
             A{rx:0.##},{ry:0.##} 0 1 1 {cx:0.##},{cy + ry:0.##}
             A{rx:0.##},{ry:0.##} 0 1 1 {cx:0.##},{cy - ry:0.##} Z
             """);

    static string Diamond(double cx, double cy, double width, double height)
    {
        var w2 = width / 2;
        var h2 = height / 2;
        return string.Create(
            Inv,
            $"""
             M{cx:0.##},{cy - h2:0.##}
             L{cx + w2:0.##},{cy:0.##}
             L{cx:0.##},{cy + h2:0.##}
             L{cx - w2:0.##},{cy:0.##} Z
             """);
    }

    static string Hexagon(double cx, double cy, double width, double height)
    {
        var w4 = width / 4;
        var w2 = width / 2;
        var h2 = height / 2;
        return string.Create(
            Inv,
            $"""
             M{cx - w4:0.##},{cy - h2:0.##}
             L{cx + w4:0.##},{cy - h2:0.##}
             L{cx + w2:0.##},{cy:0.##}
             L{cx + w4:0.##},{cy + h2:0.##}
             L{cx - w4:0.##},{cy + h2:0.##}
             L{cx - w2:0.##},{cy:0.##} Z
             """);
    }

    static string Stadium(double x, double y, double width, double height)
    {
        var r = height / 2;
        return string.Create(
            Inv,
            $"""
             M{x + r:0.##},{y:0.##}
             H{x + width - r:0.##}
             A{r:0.##},{r:0.##} 0 0 1 {x + width - r:0.##},{y + height:0.##}
             H{x + r:0.##}
             A{r:0.##},{r:0.##} 0 0 1 {x + r:0.##},{y:0.##} Z
             """);
    }

    static string Cylinder(double x, double y, double width, double height)
    {
        var ry = height * 0.1;
        var bodyHeight = height - ry * 2;
        var rx = width / 2;
        return string.Create(
            Inv,
            $"""
             M{x:0.##},{y + ry:0.##}
             A{rx:0.##},{ry:0.##} 0 0 1 {x + width:0.##},{y + ry:0.##}
             V{y + ry + bodyHeight:0.##}
             A{rx:0.##},{ry:0.##} 0 0 1 {x:0.##},{y + ry + bodyHeight:0.##} V{y + ry:0.##}
             Z M{x:0.##},{y + ry:0.##}
             A{rx:0.##},{ry:0.##} 0 0 0 {x + width:0.##},{y + ry:0.##}
             """);
    }

    static string Parallelogram(double x, double y, double width, double height, double skew = 0.2)
    {
        var offset = width * skew;
        return string.Create(
            Inv,
            $"""
             M{x + offset:0.##},{y:0.##}
             L{x + width:0.##},{y:0.##}
             L{x + width - offset:0.##},{y + height:0.##}
             L{x:0.##},{y + height:0.##} Z
             """);
    }

    static string ParallelogramAlt(double x, double y, double width, double height, double skew = 0.2)
    {
        var offset = width * skew;
        return string.Create(
            Inv,
            $"""
             M{x:0.##},{y:0.##}
             L{x + width - offset:0.##},{y:0.##}
             L{x + width:0.##},{y + height:0.##}
             L{x + offset:0.##},{y + height:0.##} Z
             """);
    }

    static string Trapezoid(double x, double y, double width, double height, double skew = 0.2)
    {
        var offset = width * skew;
        return string.Create(
            Inv,
            $"""
             M{x + offset:0.##},{y:0.##}
             L{x + width - offset:0.##},{y:0.##}
             L{x + width:0.##},{y + height:0.##}
             L{x:0.##},{y + height:0.##} Z
             """);
    }

    static string TrapezoidAlt(double x, double y, double width, double height, double skew = 0.2)
    {
        var offset = width * skew;
        return string.Create(
            Inv,
            $"""
             M{x:0.##},{y:0.##}
             L{x + width:0.##},{y:0.##}
             L{x + width - offset:0.##},{y + height:0.##} {x + offset:0.##},{y + height:0.##} Z
             """);
    }

    static string Asymmetric(double x, double y, double width, double height)
    {
        var notch = width * 0.15;
        return string.Create(
            Inv,
            $"""
             M{x + notch:0.##},{y:0.##}
             L{x + width:0.##},{y:0.##}
             L{x + width:0.##},{y + height:0.##}
             L{x + notch:0.##},{y + height:0.##}
             L{x:0.##},{y + height / 2:0.##} Z
             """);
    }

    static string Subroutine(double x, double y, double width, double height)
    {
        var inset = width * 0.1;
        return string.Create(
            Inv,
            $"""
             M{x:0.##},{y:0.##}
             H{x + width:0.##}
             V{y + height:0.##} H{x:0.##}
             Z
             M{x + inset:0.##},{y:0.##}
             V{y + height:0.##}
             M{x + width - inset:0.##},{y:0.##}
             V{y + height:0.##}
             """);
    }

    static string DoubleCircle(double cx, double cy, double r)
    {
        var innerR = r * 0.85;
        return $"{Circle(cx, cy, r)} {Circle(cx, cy, innerR)}";
    }

    static string Document(double x, double y, double width, double height)
    {
        var waveHeight = height * 0.15;
        var bodyHeight = height - waveHeight;
        var xWidth = x + width;
        return string.Create(
            Inv,
            $"""
             M{x:0.##},{y:0.##}
             H{xWidth:0.##}
             V{y + bodyHeight:0.##}
             Q{x + width * 0.75:0.##},{y + height + waveHeight * 0.5:0.##} {x + width * 0.5:0.##},{y + bodyHeight:0.##}
             Q{x + width * 0.25:0.##},{y + bodyHeight - waveHeight * 0.5:0.##} {x:0.##},{y + bodyHeight:0.##} Z
             """);
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
}
