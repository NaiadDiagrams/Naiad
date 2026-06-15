using System.Numerics;

namespace Naiad.Rendering;

/// <summary>
/// Parses SVG path <c>d</c> data and flattens it to polyline <see cref="SubPath"/>s. Every command in
/// the grammar is handled — <c>M/L/H/V/C/S/Q/T/A/Z</c> and their relative lowercase forms, including
/// implicit command repetition and the compact number syntax (e.g. <c>1-2</c>, <c>.5.5</c>). Cubic and
/// quadratic Béziers are subdivided to a flatness tolerance and elliptical arcs are converted from
/// endpoint to centre parameterisation and sampled, so each backend only ever receives line segments.
/// </summary>
static class PathFlattener
{
    // Flatness tolerance in user units. Curves subdivide until within this of a straight chord; small
    // enough that the polyline reads as smooth at the scales Naiad renders, large enough to keep the
    // vertex count modest.
    const double tolerance = 0.2;

    public static List<SubPath> Flatten(string? d)
    {
        var result = new List<SubPath>();
        if (string.IsNullOrWhiteSpace(d))
        {
            return result;
        }

        var scanner = new Scanner(d);
        List<Vector2>? sub = null;
        var current = Vector2.Zero;
        var start = Vector2.Zero;
        var command = '\0';
        Vector2? lastCubicControl = null;
        Vector2? lastQuadControl = null;

        void Open()
        {
            if (sub == null)
            {
                sub = [current];
                result.Add(new(sub, false));
                start = current;
            }
        }

        while (scanner.TryReadCommand(out var next))
        {
            command = next;
            var relative = char.IsLower(command);
            switch (char.ToUpperInvariant(command))
            {
                case 'M':
                    var move = scanner.ReadPoint();
                    current = relative ? current + move : move;
                    sub = [current];
                    result.Add(new(sub, false));
                    start = current;
                    lastCubicControl = lastQuadControl = null;
                    // Subsequent implicit pairs after a moveto are linetos.
                    while (scanner.PeekNumber())
                    {
                        var to = scanner.ReadPoint();
                        current = relative ? current + to : to;
                        sub.Add(current);
                    }

                    lastCubicControl = lastQuadControl = null;
                    break;
                case 'L':
                    Open();
                    do
                    {
                        var to = scanner.ReadPoint();
                        current = relative ? current + to : to;
                        sub!.Add(current);
                    }
                    while (scanner.PeekNumber());

                    lastCubicControl = lastQuadControl = null;
                    break;
                case 'H':
                    Open();
                    do
                    {
                        var x = scanner.ReadNumber();
                        current = current with {X = relative ? current.X + (float)x : (float)x};
                        sub!.Add(current);
                    }
                    while (scanner.PeekNumber());

                    lastCubicControl = lastQuadControl = null;
                    break;
                case 'V':
                    Open();
                    do
                    {
                        var y = scanner.ReadNumber();
                        current = current with {Y = relative ? current.Y + (float)y : (float)y};
                        sub!.Add(current);
                    }
                    while (scanner.PeekNumber());

                    lastCubicControl = lastQuadControl = null;
                    break;
                case 'C':
                    Open();
                    do
                    {
                        var c1 = Resolve(scanner.ReadPoint(), current, relative);
                        var c2 = Resolve(scanner.ReadPoint(), current, relative);
                        var end = Resolve(scanner.ReadPoint(), current, relative);
                        Cubic(sub!, current, c1, c2, end);
                        lastCubicControl = c2;
                        current = end;
                    }
                    while (scanner.PeekNumber());

                    lastQuadControl = null;
                    break;
                case 'S':
                    Open();
                    do
                    {
                        var c1 = lastCubicControl is { } prev ? current + (current - prev) : current;
                        var c2 = Resolve(scanner.ReadPoint(), current, relative);
                        var end = Resolve(scanner.ReadPoint(), current, relative);
                        Cubic(sub!, current, c1, c2, end);
                        lastCubicControl = c2;
                        current = end;
                    }
                    while (scanner.PeekNumber());

                    lastQuadControl = null;
                    break;
                case 'Q':
                    Open();
                    do
                    {
                        var c = Resolve(scanner.ReadPoint(), current, relative);
                        var end = Resolve(scanner.ReadPoint(), current, relative);
                        Quadratic(sub!, current, c, end);
                        lastQuadControl = c;
                        current = end;
                    }
                    while (scanner.PeekNumber());

                    lastCubicControl = null;
                    break;
                case 'T':
                    Open();
                    do
                    {
                        var c = lastQuadControl is { } prev ? current + (current - prev) : current;
                        var end = Resolve(scanner.ReadPoint(), current, relative);
                        Quadratic(sub!, current, c, end);
                        lastQuadControl = c;
                        current = end;
                    }
                    while (scanner.PeekNumber());

                    lastCubicControl = null;
                    break;
                case 'A':
                    Open();
                    do
                    {
                        var rx = scanner.ReadNumber();
                        var ry = scanner.ReadNumber();
                        var angle = scanner.ReadNumber();
                        var largeArc = scanner.ReadFlag();
                        var sweep = scanner.ReadFlag();
                        var end = Resolve(scanner.ReadPoint(), current, relative);
                        Arc(sub!, current, rx, ry, angle, largeArc, sweep, end);
                        current = end;
                    }
                    while (scanner.PeekNumber());

                    lastCubicControl = lastQuadControl = null;
                    break;
                case 'Z':
                    if (sub != null)
                    {
                        result[^1] = new(sub, true);
                        current = start;
                        sub = null;
                    }

                    lastCubicControl = lastQuadControl = null;
                    break;
            }
        }

        _ = command;
        return result;
    }

    static Vector2 Resolve(Vector2 point, Vector2 current, bool relative) =>
        relative ? current + point : point;

    static void Cubic(List<Vector2> output, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, int depth = 0)
    {
        // Subdivide until the control points sit within the flatness tolerance of the chord, then emit
        // the endpoint. The depth cap is a guard against pathological inputs.
        if (depth >= 18 || Flat(p0, p1, p2, p3))
        {
            output.Add(p3);
            return;
        }

        var p01 = Mid(p0, p1);
        var p12 = Mid(p1, p2);
        var p23 = Mid(p2, p3);
        var p012 = Mid(p01, p12);
        var p123 = Mid(p12, p23);
        var mid = Mid(p012, p123);
        Cubic(output, p0, p01, p012, mid, depth + 1);
        Cubic(output, mid, p123, p23, p3, depth + 1);
    }

    static void Quadratic(List<Vector2> output, Vector2 p0, Vector2 c, Vector2 p1, int depth = 0)
    {
        if (depth >= 18 || DistanceToLine(c, p0, p1) <= tolerance)
        {
            output.Add(p1);
            return;
        }

        var p0c = Mid(p0, c);
        var cp1 = Mid(c, p1);
        var mid = Mid(p0c, cp1);
        Quadratic(output, p0, p0c, mid, depth + 1);
        Quadratic(output, mid, cp1, p1, depth + 1);
    }

    static bool Flat(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3) =>
        DistanceToLine(p1, p0, p3) <= tolerance && DistanceToLine(p2, p0, p3) <= tolerance;

    static double DistanceToLine(Vector2 point, Vector2 a, Vector2 b)
    {
        var dx = b.X - a.X;
        var dy = b.Y - a.Y;
        var lengthSq = dx * dx + dy * dy;
        if (lengthSq < 1e-9)
        {
            return Vector2.Distance(point, a);
        }

        // Perpendicular distance from point to the infinite line through a and b.
        var cross = Math.Abs((point.X - a.X) * dy - (point.Y - a.Y) * dx);
        return cross / Math.Sqrt(lengthSq);
    }

    static Vector2 Mid(Vector2 a, Vector2 b) =>
        new((a.X + b.X) / 2, (a.Y + b.Y) / 2);

    // Endpoint → centre parameterisation (SVG implementation notes F.6), then sample by angle.
    static void Arc(List<Vector2> output, Vector2 from, double rx, double ry, double xAngle, bool largeArc, bool sweep, Vector2 to)
    {
        if (rx == 0 || ry == 0 || from == to)
        {
            output.Add(to);
            return;
        }

        rx = Math.Abs(rx);
        ry = Math.Abs(ry);
        var phi = xAngle * Math.PI / 180;
        var cosPhi = Math.Cos(phi);
        var sinPhi = Math.Sin(phi);

        var dx = (from.X - to.X) / 2;
        var dy = (from.Y - to.Y) / 2;
        var x1 = cosPhi * dx + sinPhi * dy;
        var y1 = -sinPhi * dx + cosPhi * dy;

        // Scale the radii up if they're too small to span the endpoints (SVG F.6.6).
        var lambda = x1 * x1 / (rx * rx) + y1 * y1 / (ry * ry);
        if (lambda > 1)
        {
            var scale = Math.Sqrt(lambda);
            rx *= scale;
            ry *= scale;
        }

        var sign = largeArc == sweep ? -1 : 1;
        var numerator = rx * rx * ry * ry - rx * rx * y1 * y1 - ry * ry * x1 * x1;
        var denominator = rx * rx * y1 * y1 + ry * ry * x1 * x1;
        var coefficient = sign * Math.Sqrt(Math.Max(0, numerator / denominator));

        var cx1 = coefficient * rx * y1 / ry;
        var cy1 = -coefficient * ry * x1 / rx;
        var cx = cosPhi * cx1 - sinPhi * cy1 + (from.X + to.X) / 2;
        var cy = sinPhi * cx1 + cosPhi * cy1 + (from.Y + to.Y) / 2;

        var startAngle = Angle(1, 0, (x1 - cx1) / rx, (y1 - cy1) / ry);
        var deltaAngle = Angle((x1 - cx1) / rx, (y1 - cy1) / ry, (-x1 - cx1) / rx, (-y1 - cy1) / ry);
        if (!sweep && deltaAngle > 0)
        {
            deltaAngle -= 2 * Math.PI;
        }
        else if (sweep && deltaAngle < 0)
        {
            deltaAngle += 2 * Math.PI;
        }

        var segments = Math.Max(2, (int)Math.Ceiling(Math.Abs(deltaAngle) / (Math.PI / 32)));
        for (var i = 1; i <= segments; i++)
        {
            var theta = startAngle + deltaAngle * i / segments;
            var ex = cosPhi * rx * Math.Cos(theta) - sinPhi * ry * Math.Sin(theta) + cx;
            var ey = sinPhi * rx * Math.Cos(theta) + cosPhi * ry * Math.Sin(theta) + cy;
            output.Add(new((float)ex, (float)ey));
        }
    }

    static double Angle(double ux, double uy, double vx, double vy)
    {
        var dot = ux * vx + uy * vy;
        var length = Math.Sqrt((ux * ux + uy * uy) * (vx * vx + vy * vy));
        var angle = Math.Acos(Math.Clamp(dot / length, -1, 1));
        return ux * vy - uy * vx < 0 ? -angle : angle;
    }

    // Cursor over path data: yields command letters, numbers, points and arc flags, treating commas
    // and whitespace as interchangeable separators and supporting the compact "1-2" / ".5.5" syntax.
    sealed class Scanner(string text)
    {
        int position;

        public bool TryReadCommand(out char command)
        {
            SkipSeparators();
            while (position < text.Length)
            {
                var c = text[position];
                if (char.IsLetter(c))
                {
                    position++;
                    command = c;
                    return true;
                }

                // A bare number where a command is expected repeats the previous command; the caller
                // handles repetition via PeekNumber, so anything reaching here that isn't a command is
                // skipped to stay robust against malformed input.
                break;
            }

            command = '\0';
            return false;
        }

        public bool PeekNumber()
        {
            SkipSeparators();
            if (position >= text.Length)
            {
                return false;
            }

            var c = text[position];
            return c is '-' or '+' or '.' or (>= '0' and <= '9');
        }

        public Vector2 ReadPoint() =>
            new((float)ReadNumber(), (float)ReadNumber());

        public double ReadNumber()
        {
            SkipSeparators();
            var startIndex = position;
            if (position < text.Length && text[position] is '-' or '+')
            {
                position++;
            }

            while (position < text.Length && text[position] is >= '0' and <= '9')
            {
                position++;
            }

            if (position < text.Length && text[position] == '.')
            {
                position++;
                while (position < text.Length && text[position] is >= '0' and <= '9')
                {
                    position++;
                }
            }

            if (position < text.Length && text[position] is 'e' or 'E')
            {
                position++;
                if (position < text.Length && text[position] is '-' or '+')
                {
                    position++;
                }

                while (position < text.Length && text[position] is >= '0' and <= '9')
                {
                    position++;
                }
            }

            var slice = text.AsSpan(startIndex, position - startIndex);
            return double.TryParse(slice, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ? value : 0;
        }

        // Arc large-arc/sweep flags are single '0' or '1' digits that may abut the next number with no
        // separator, so they get their own minimal reader.
        public bool ReadFlag()
        {
            SkipSeparators();
            if (position < text.Length)
            {
                var c = text[position];
                position++;
                return c == '1';
            }

            return false;
        }

        void SkipSeparators()
        {
            while (position < text.Length)
            {
                var c = text[position];
                if (c is ' ' or '\t' or '\n' or '\r' or ',')
                {
                    position++;
                }
                else
                {
                    break;
                }
            }
        }
    }
}
