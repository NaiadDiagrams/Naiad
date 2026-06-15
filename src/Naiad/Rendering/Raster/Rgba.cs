/// <summary>
/// A straight-alpha 8-bit-per-channel colour. This is the colour currency shared between the SVG
/// walker and the render surfaces (<see cref="IRenderSurface"/>); each backend converts it to its
/// own colour type at the point of drawing.
/// </summary>
readonly record struct Rgba(byte R, byte G, byte B, byte A)
{
    public static Rgba White => new(255, 255, 255, 255);

    public static Rgba Black => new(0, 0, 0, 255);

    public static Rgba Transparent => new(0, 0, 0, 0);

    /// <summary>Returns this colour with its alpha scaled by <paramref name="factor"/> (clamped to [0, 1]).</summary>
    public Rgba MultiplyAlpha(double factor)
    {
        if (factor >= 1)
        {
            return this;
        }

        var scaled = (int)Math.Round(A * Math.Clamp(factor, 0, 1));
        return this with {A = (byte)Math.Clamp(scaled, 0, 255)};
    }
}
