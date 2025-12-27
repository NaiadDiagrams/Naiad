namespace MermaidSharp.Core.Models;

public class Style
{
    public string? Fill { get; set; }
    public string? Stroke { get; set; }
    public double? StrokeWidth { get; set; }
    public string? StrokeDasharray { get; set; }
    public string? FontFamily { get; set; }
    public double? FontSize { get; set; }
    public string? FontWeight { get; set; }
    public string? TextColor { get; set; }

    public string ToCss()
    {
        var parts = new List<string>();
        if (Fill is not null) parts.Add($"fill:{Fill}");
        if (Stroke is not null) parts.Add($"stroke:{Stroke}");
        if (StrokeWidth.HasValue) parts.Add($"stroke-width:{StrokeWidth}");
        if (StrokeDasharray is not null) parts.Add($"stroke-dasharray:{StrokeDasharray}");
        if (FontFamily is not null) parts.Add($"font-family:{FontFamily}");
        if (FontSize.HasValue) parts.Add($"font-size:{FontSize}px");
        if (FontWeight is not null) parts.Add($"font-weight:{FontWeight}");
        if (TextColor is not null) parts.Add($"color:{TextColor}");
        return string.Join(";", parts);
    }
}
