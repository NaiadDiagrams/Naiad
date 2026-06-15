namespace Naiad;

public class RenderOptions
{
    public static RenderOptions Default => new();

    public double Padding { get; set; } = 20;
    public int FontSize { get; set; } = 14;
    public string FontFamily { get; set; } = "Arial, sans-serif";

    // When false, labels are emitted as native SVG <text> instead of <foreignObject>
    // (and the Font Awesome @import is dropped), so the SVG renders without an HTML runtime.
    public bool AllowHtmlElements { get; set; } = true;
}
