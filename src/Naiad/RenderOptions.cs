namespace Naiad;

public class RenderOptions
{
    public static RenderOptions Default => new();

    public double Padding { get; set; } = 20;
    public int FontSize { get; set; } = 14;
    public string FontFamily { get; set; } = "Arial, sans-serif";
    public bool ShowBoundingBox { get; set; }
    public bool AllowHtmlElements { get; set; } = true;
}
