/// <summary>
/// Routes a cross-cluster edge from one point to another around rectangular obstacles (the other cluster
/// boxes), via a visibility graph over the obstacle corners with a shortest-path search. The returned
/// waypoints hug the gutters between boxes instead of cutting straight across them; the caller smooths
/// them into a curve. Falls back to a straight segment when the direct line is already clear or no path
/// is found.
/// </summary>
static class BoxRouter
{
    public readonly record struct Box(double MinX, double MinY, double MaxX, double MaxY);

    public static List<Position> Route(Position start, Position end, IReadOnlyList<Box> obstacles)
    {
        if (Visible(start, end, obstacles))
        {
            return [start, end];
        }

        var points = new List<Position> { start, end };
        foreach (var box in obstacles)
        {
            points.Add(new(box.MinX, box.MinY));
            points.Add(new(box.MaxX, box.MinY));
            points.Add(new(box.MinX, box.MaxY));
            points.Add(new(box.MaxX, box.MaxY));
        }

        var count = points.Count;
        var dist = new double[count];
        var prev = new int[count];
        var done = new bool[count];
        for (var i = 0; i < count; i++)
        {
            dist[i] = double.PositiveInfinity;
            prev[i] = -1;
        }

        dist[0] = 0;

        // Dijkstra over the visibility graph (index 0 = start, 1 = end).
        for (var iteration = 0; iteration < count; iteration++)
        {
            var u = -1;
            var best = double.PositiveInfinity;
            for (var i = 0; i < count; i++)
            {
                if (!done[i] && dist[i] < best)
                {
                    best = dist[i];
                    u = i;
                }
            }

            if (u < 0 || u == 1)
            {
                break;
            }

            done[u] = true;
            for (var v = 0; v < count; v++)
            {
                if (done[v] || v == u || !Visible(points[u], points[v], obstacles))
                {
                    continue;
                }

                var candidate = dist[u] + Distance(points[u], points[v]);
                if (candidate < dist[v])
                {
                    dist[v] = candidate;
                    prev[v] = u;
                }
            }
        }

        if (prev[1] < 0)
        {
            return [start, end];
        }

        var path = new List<Position>();
        for (var at = 1; at >= 0; at = prev[at])
        {
            path.Add(points[at]);
            if (at == 0)
            {
                break;
            }
        }

        path.Reverse();
        return path;
    }

    static bool Visible(Position a, Position b, IReadOnlyList<Box> obstacles)
    {
        foreach (var box in obstacles)
        {
            if (SegmentEntersBox(a, b, box))
            {
                return false;
            }
        }

        return true;
    }

    // Liang-Barsky: true if the segment a-b passes through the box's interior (grazing the boundary is allowed,
    // so corner-to-corner paths along a box edge stay visible).
    static bool SegmentEntersBox(Position a, Position b, Box box)
    {
        const double eps = 0.5;
        var minX = box.MinX + eps;
        var minY = box.MinY + eps;
        var maxX = box.MaxX - eps;
        var maxY = box.MaxY - eps;
        if (maxX <= minX || maxY <= minY)
        {
            return false;
        }

        var dx = b.X - a.X;
        var dy = b.Y - a.Y;
        double t0 = 0;
        double t1 = 1;

        Span<double> p = [-dx, dx, -dy, dy];
        Span<double> q = [a.X - minX, maxX - a.X, a.Y - minY, maxY - a.Y];

        for (var i = 0; i < 4; i++)
        {
            if (p[i] == 0)
            {
                if (q[i] < 0)
                {
                    return false;
                }
            }
            else
            {
                var t = q[i] / p[i];
                if (p[i] < 0)
                {
                    if (t > t0)
                    {
                        t0 = t;
                    }
                }
                else if (t < t1)
                {
                    t1 = t;
                }
            }
        }

        return t0 < t1;
    }

    static double Distance(Position a, Position b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }
}
