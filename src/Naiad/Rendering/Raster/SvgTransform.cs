/// <summary>
/// Parses an SVG <c>transform</c> attribute (<c>translate</c>, <c>scale</c>, <c>rotate</c>,
/// <c>matrix</c>, <c>skewX</c>, <c>skewY</c>, in any combination) into a single
/// <see cref="Matrix3x2"/>. Matrices use the row-vector convention (<c>v' = v * M</c>), matching
/// <see cref="System.Numerics"/>; a transform list is composed so the leftmost entry is the outermost
/// transform, exactly as SVG specifies.
/// </summary>
static partial class SvgTransform
{
    public static Matrix3x2 Parse(string? transform)
    {
        if (string.IsNullOrWhiteSpace(transform))
        {
            return Matrix3x2.Identity;
        }

        var combined = Matrix3x2.Identity;
        foreach (Match match in FunctionRegex().Matches(transform))
        {
            var name = match.Groups["name"].Value;
            var args = ParseArgs(match.Groups["args"].Value);
            if (Build(name, args) is { } op)
            {
                // Document order: each successive entry is the outer transform of the ones before it,
                // so pre-multiply (op then accumulated) — see the class remarks.
                combined = op * combined;
            }
        }

        return combined;
    }

    static Matrix3x2? Build(string name, double[] a) =>
        name switch
        {
            "translate" => a.Length switch
            {
                >= 2 => Matrix3x2.CreateTranslation((float)a[0], (float)a[1]),
                1 => Matrix3x2.CreateTranslation((float)a[0], 0),
                _ => null,
            },
            "scale" => a.Length switch
            {
                >= 2 => Matrix3x2.CreateScale((float)a[0], (float)a[1]),
                1 => Matrix3x2.CreateScale((float)a[0]),
                _ => null,
            },
            "rotate" => a.Length switch
            {
                >= 3 => Matrix3x2.CreateRotation(Radians(a[0]), new((float)a[1], (float)a[2])),
                >= 1 => Matrix3x2.CreateRotation(Radians(a[0])),
                _ => null,
            },
            "matrix" when a.Length >= 6 =>
                new((float)a[0], (float)a[1], (float)a[2], (float)a[3], (float)a[4], (float)a[5]),
            "skewX" when a.Length >= 1 =>
                new(1, 0, (float)Math.Tan(Radians(a[0])), 1, 0, 0),
            "skewY" when a.Length >= 1 =>
                new(1, (float)Math.Tan(Radians(a[0])), 0, 1, 0, 0),
            _ => null,
        };

    static float Radians(double degrees) =>
        (float)(degrees * Math.PI / 180);

    static double[] ParseArgs(string args)
    {
        var parts = args.Split([',', ' ', '\t', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var values = new double[parts.Length];
        var count = 0;
        foreach (var part in parts)
        {
            if (double.TryParse(part, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            {
                values[count++] = value;
            }
        }

        return count == values.Length ? values : values[..count];
    }

    [GeneratedRegex(@"(?<name>\w+)\s*\((?<args>[^)]*)\)")]
    private static partial Regex FunctionRegex();
}
