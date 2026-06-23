/// <summary>
/// Parses an SVG <c>transform</c> attribute (<c>translate</c>, <c>scale</c>, <c>rotate</c>,
/// <c>matrix</c>, <c>skewX</c>, <c>skewY</c>, in any combination) into a single
/// <see cref="Matrix3x2"/>. Matrices use the row-vector convention (<c>v' = v * M</c>), matching
/// <see cref="System.Numerics"/>; a transform list is composed so the leftmost entry is the outermost
/// transform, exactly as SVG specifies.
/// </summary>
static class SvgTransform
{
    public static Matrix3x2 Parse(string? transform)
    {
        if (string.IsNullOrWhiteSpace(transform))
        {
            return Matrix3x2.Identity;
        }

        // Hand-rolled span scan over "name(args) name(args) ...": avoids the regex (MatchCollection/Match/
        // Group/captured strings), the args Split array + substrings, and the per-function double[] that the
        // old parser allocated on every transformed element. The arg buffer is reused across functions.
        var combined = Matrix3x2.Identity;
        var s = transform.AsSpan();
        Span<double> args = stackalloc double[6];

        var i = 0;
        while (i < s.Length)
        {
            if (!IsNameChar(s[i]))
            {
                i++;
                continue;
            }

            var nameStart = i;
            while (i < s.Length && IsNameChar(s[i]))
            {
                i++;
            }

            var name = s[nameStart..i];

            while (i < s.Length && char.IsWhiteSpace(s[i]))
            {
                i++;
            }

            if (i >= s.Length || s[i] != '(')
            {
                // A bare identifier with no argument list — skip it and keep scanning.
                continue;
            }

            i++; // consume '('
            var argsStart = i;
            while (i < s.Length && s[i] != ')')
            {
                i++;
            }

            var count = ParseArgs(s[argsStart..i], args);
            if (i < s.Length)
            {
                i++; // consume ')'
            }

            if (Build(name, args[..count]) is { } op)
            {
                // Document order: each successive entry is the outer transform of the ones before it,
                // so pre-multiply (op then accumulated) — see the class remarks.
                combined = op * combined;
            }
        }

        return combined;
    }

    static Matrix3x2? Build(CharSpan name, ReadOnlySpan<double> a) =>
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

    // Parses up to dest.Length numbers separated by commas/whitespace into dest; returns how many were read.
    static int ParseArgs(CharSpan args, Span<double> dest)
    {
        var count = 0;
        var i = 0;
        while (i < args.Length && count < dest.Length)
        {
            while (i < args.Length && IsSeparator(args[i]))
            {
                i++;
            }

            if (i >= args.Length)
            {
                break;
            }

            var start = i;
            while (i < args.Length && !IsSeparator(args[i]))
            {
                i++;
            }

            if (double.TryParse(args[start..i], NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            {
                dest[count++] = value;
            }
        }

        return count;
    }

    // Mirrors the regex \w (identifier) for the function name.
    static bool IsNameChar(char c) => char.IsLetterOrDigit(c) ||
                                      c == '_';

    static bool IsSeparator(char c) => c is
        ',' or
        ' ' or
        '\t' or
        '\n' or
        '\r';
}
