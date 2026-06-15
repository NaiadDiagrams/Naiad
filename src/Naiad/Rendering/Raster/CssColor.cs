namespace Naiad.Rendering;

/// <summary>
/// Parses the CSS colour syntaxes that appear in Naiad/Mermaid output — hex (<c>#rgb</c>,
/// <c>#rgba</c>, <c>#rrggbb</c>, <c>#rrggbbaa</c>), <c>rgb()</c>/<c>rgba()</c>,
/// <c>hsl()</c>/<c>hsla()</c> and the named colours — into an <see cref="Rgba"/>. Returns false for
/// <c>none</c>, <c>currentColor</c> (resolved by the caller) and anything unrecognised, so callers can
/// treat "no concrete paint" as an ordinary outcome.
/// </summary>
static class CssColor
{
    public static bool TryParse(string? text, out Rgba color)
    {
        color = default;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var value = text.Trim();

        if (value.Equals("none", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("currentColor", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (value[0] == '#')
        {
            return TryParseHex(value.AsSpan(1), out color);
        }

        if (value.StartsWith("rgb", StringComparison.OrdinalIgnoreCase))
        {
            return TryParseRgb(value, out color);
        }

        if (value.StartsWith("hsl", StringComparison.OrdinalIgnoreCase))
        {
            return TryParseHsl(value, out color);
        }

        return TryParseNamed(value, out color);
    }

    static bool TryParseHex(CharSpan hex, out Rgba color)
    {
        color = default;
        switch (hex.Length)
        {
            case 3:
                // #rgb → #rrggbb
                if (Nibble(hex[0], out var r3) && Nibble(hex[1], out var g3) && Nibble(hex[2], out var b3))
                {
                    color = new((byte)(r3 * 17), (byte)(g3 * 17), (byte)(b3 * 17), 255);
                    return true;
                }

                return false;
            case 4:
                if (Nibble(hex[0], out var r4) && Nibble(hex[1], out var g4) && Nibble(hex[2], out var b4) && Nibble(hex[3], out var a4))
                {
                    color = new((byte)(r4 * 17), (byte)(g4 * 17), (byte)(b4 * 17), (byte)(a4 * 17));
                    return true;
                }

                return false;
            case 6:
                if (Byte(hex[..2], out var r6) && Byte(hex[2..4], out var g6) && Byte(hex[4..6], out var b6))
                {
                    color = new(r6, g6, b6, 255);
                    return true;
                }

                return false;
            case 8:
                if (Byte(hex[..2], out var r8) && Byte(hex[2..4], out var g8) && Byte(hex[4..6], out var b8) && Byte(hex[6..8], out var a8))
                {
                    color = new(r8, g8, b8, a8);
                    return true;
                }

                return false;
            default:
                return false;
        }
    }

    static bool TryParseRgb(string value, out Rgba color)
    {
        color = default;
        var parts = Components(value);
        if (parts is not {Length: 3 or 4} components)
        {
            return false;
        }

        if (!Channel(components[0], out var r) ||
            !Channel(components[1], out var g) ||
            !Channel(components[2], out var b))
        {
            return false;
        }

        var a = (byte)255;
        if (components.Length == 4 && TryAlpha(components[3], out var alpha))
        {
            a = alpha;
        }

        color = new(r, g, b, a);
        return true;
    }

    static bool TryParseHsl(string value, out Rgba color)
    {
        color = default;
        var parts = Components(value);
        if (parts is not {Length: 3 or 4} components)
        {
            return false;
        }

        if (!double.TryParse(components[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var h) ||
            !Percent(components[1], out var s) ||
            !Percent(components[2], out var l))
        {
            return false;
        }

        var a = (byte)255;
        if (components.Length == 4 && TryAlpha(components[3], out var alpha))
        {
            a = alpha;
        }

        var (r, g, b) = HslToRgb(h, s, l);
        color = new(r, g, b, a);
        return true;
    }

    // Standard HSL→RGB conversion (CSS Color 3). h in degrees, s/l in [0, 1].
    static (byte R, byte G, byte B) HslToRgb(double h, double s, double l)
    {
        h = (h % 360 + 360) % 360 / 360;
        var q = l < 0.5 ? l * (1 + s) : l + s - l * s;
        var p = 2 * l - q;
        var r = HueToChannel(p, q, h + 1d / 3);
        var g = HueToChannel(p, q, h);
        var b = HueToChannel(p, q, h - 1d / 3);
        return (Component(r), Component(g), Component(b));
    }

    static double HueToChannel(double p, double q, double t)
    {
        t = (t % 1 + 1) % 1;
        if (t < 1d / 6)
        {
            return p + (q - p) * 6 * t;
        }

        if (t < 1d / 2)
        {
            return q;
        }

        if (t < 2d / 3)
        {
            return p + (q - p) * (2d / 3 - t) * 6;
        }

        return p;
    }

    static string[]? Components(string value)
    {
        var open = value.IndexOf('(');
        var close = value.LastIndexOf(')');
        if (open < 0 || close <= open)
        {
            return null;
        }

        var inner = value[(open + 1)..close];
        // Both comma and whitespace separators occur in the wild; split on either.
        return inner.Split([',', ' ', '/'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    static bool Channel(string text, out byte value)
    {
        value = 0;
        if (text.EndsWith('%'))
        {
            if (Percent(text, out var fraction))
            {
                value = Component(fraction);
                return true;
            }

            return false;
        }

        if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
        {
            value = Component(number / 255);
            return true;
        }

        return false;
    }

    static bool TryAlpha(string text, out byte value)
    {
        value = 255;
        if (text.EndsWith('%'))
        {
            if (Percent(text, out var fraction))
            {
                value = Component(fraction);
                return true;
            }

            return false;
        }

        if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
        {
            value = Component(number);
            return true;
        }

        return false;
    }

    static bool Percent(string text, out double fraction)
    {
        fraction = 0;
        var trimmed = text.EndsWith('%') ? text[..^1] : text;
        if (double.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
        {
            fraction = text.EndsWith('%') ? number / 100 : number;
            return true;
        }

        return false;
    }

    static byte Component(double fraction) =>
        (byte)Math.Clamp((int)Math.Round(fraction * 255), 0, 255);

    static bool Nibble(char c, out int value)
    {
        if (c is >= '0' and <= '9')
        {
            value = c - '0';
            return true;
        }

        if (c is >= 'a' and <= 'f')
        {
            value = c - 'a' + 10;
            return true;
        }

        if (c is >= 'A' and <= 'F')
        {
            value = c - 'A' + 10;
            return true;
        }

        value = 0;
        return false;
    }

    static bool Byte(CharSpan hex, out byte value)
    {
        if (Nibble(hex[0], out var hi) && Nibble(hex[1], out var lo))
        {
            value = (byte)(hi * 16 + lo);
            return true;
        }

        value = 0;
        return false;
    }

    static bool TryParseNamed(string name, out Rgba color) =>
        named.TryGetValue(name, out color);

    // The CSS named colours that turn up in Mermaid stylesheets and palettes, plus the common ones a
    // user-supplied theme might reach for. Not the full 147-entry table — just a practical subset.
    static readonly Dictionary<string, Rgba> named = new(StringComparer.OrdinalIgnoreCase)
    {
        ["transparent"] = new(0, 0, 0, 0),
        ["black"] = new(0, 0, 0, 255),
        ["white"] = new(255, 255, 255, 255),
        ["red"] = new(255, 0, 0, 255),
        ["green"] = new(0, 128, 0, 255),
        ["blue"] = new(0, 0, 255, 255),
        ["yellow"] = new(255, 255, 0, 255),
        ["cyan"] = new(0, 255, 255, 255),
        ["aqua"] = new(0, 255, 255, 255),
        ["magenta"] = new(255, 0, 255, 255),
        ["fuchsia"] = new(255, 0, 255, 255),
        ["gray"] = new(128, 128, 128, 255),
        ["grey"] = new(128, 128, 128, 255),
        ["silver"] = new(192, 192, 192, 255),
        ["maroon"] = new(128, 0, 0, 255),
        ["olive"] = new(128, 128, 0, 255),
        ["lime"] = new(0, 255, 0, 255),
        ["teal"] = new(0, 128, 128, 255),
        ["navy"] = new(0, 0, 128, 255),
        ["purple"] = new(128, 0, 128, 255),
        ["orange"] = new(255, 165, 0, 255),
        ["pink"] = new(255, 192, 203, 255),
        ["lightgrey"] = new(211, 211, 211, 255),
        ["lightgray"] = new(211, 211, 211, 255),
        ["darkgrey"] = new(169, 169, 169, 255),
        ["darkgray"] = new(169, 169, 169, 255),
        ["lightblue"] = new(173, 216, 230, 255),
        ["whitesmoke"] = new(245, 245, 245, 255),
        ["gold"] = new(255, 215, 0, 255),
    };
}
