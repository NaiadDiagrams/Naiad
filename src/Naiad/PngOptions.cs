namespace Naiad;

/// <summary>
/// Settings for rasterizing a diagram to PNG. Shared by the Naiad.Skia and Naiad.ImageSharp backends so
/// the two render the same way bar their rasterizer and font engine.
/// </summary>
public class PngOptions
{
    /// <summary>
    /// Device-pixel scale applied to the diagram's intrinsic (viewBox) size. <c>2</c> renders at twice
    /// the resolution for crisper output on high-DPI displays; the default of <c>1</c> matches the SVG's
    /// own coordinate size.
    /// </summary>
    public double Scale { get; set; } = 1;

    /// <summary>
    /// The canvas background, as any CSS colour (hex, <c>rgb()</c>, <c>hsl()</c> or a named colour).
    /// Diagrams are drawn assuming an opaque light background; use <c>"transparent"</c> for no fill.
    /// </summary>
    public string Background { get; set; } = "white";
}
