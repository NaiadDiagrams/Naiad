namespace Naiad;

public class RenderOptions
{
    public static RenderOptions Default => new();

    public double Padding { get; set; } = 20;
    public int FontSize { get; set; } = 14;
    public string FontFamily { get; set; } = "Arial, sans-serif";

    /// <summary>
    /// PNG rasterization settings, honoured by the Naiad.Skia and Naiad.ImageSharp render backends.
    /// Ignored by <see cref="Mermaid.Render(string, RenderOptions)"/>, which only produces SVG.
    /// </summary>
    public PngOptions Png { get; set; } = new();
}
