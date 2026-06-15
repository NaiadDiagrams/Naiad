namespace Naiad.Rendering;

enum TextAnchorKind
{
    Start,
    Middle,
    End,
}

enum TextBaselineKind
{
    /// <summary>Baseline sits on y (the SVG default).</summary>
    Alphabetic,

    /// <summary>y is the visual centre of the text (dominant-baseline middle/central).</summary>
    Middle,

    /// <summary>y is the top of the text (dominant-baseline hanging/text-before-edge).</summary>
    Hanging,
}

/// <summary>
/// The fully-resolved styling for one run of text, handed to a surface's
/// <see cref="IRenderSurface.DrawText"/>. The surface owns horizontal alignment and baseline placement
/// because both need font metrics, which only the backend's font engine has. A record so the walker can
/// derive a tweaked copy (e.g. re-anchored for foreignObject labels) with <c>with</c>.
/// </summary>
sealed record TextStyle
{
    public required IReadOnlyList<string> FontFamilies { get; init; }

    public required float FontSize { get; init; }

    public bool Bold { get; init; }

    public bool Italic { get; init; }

    public required Rgba Color { get; init; }

    public TextAnchorKind Anchor { get; init; }

    public TextBaselineKind Baseline { get; init; }

    public float Opacity { get; init; } = 1;
}
