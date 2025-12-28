
public static partial class ModuleInitializer
{
    [ModuleInitializer]
    public static void Init()
    {
        // Configure Verify to use UTF-8 without BOM
        VerifierSettings.UseUtf8NoBom();

        // Normalize floating point values to 4 decimal places for visual equivalence
        VerifierSettings.AddScrubber(NormalizeFloatingPoint);
        
        // Remove SVG from text extensions so we can use RegisterStreamConverter
        EmptyFiles.FileExtensions.RemoveTextExtension("svg");
        
        // Register SVG converter that produces both SVG and PNG outputs
        VerifierSettings.RegisterStreamConverter(
            "svg",
            (name, stream, context) =>
            {
                // Read the SVG content
                using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
                var svgContent = reader.ReadToEnd();
                
                // Create targets list with both SVG and PNG
                var targets = new List<Target>();
                
                // Add original SVG
                var svgStream = new MemoryStream(Encoding.UTF8.GetBytes(svgContent));
                targets.Add(new Target("svg", svgStream));
                
                // Convert to PNG using Svg.Skia
                var pngStream = ConvertSvgToPng(svgContent);
                if (pngStream != null)
                {
                    targets.Add(new Target("png", pngStream));
                }
                
                return new ConversionResult(null, targets);
            });
    }

    static MemoryStream? ConvertSvgToPng(string svgContent)
    {
        try
        {
            using var svg = new SKSvg();

            // Configure typeface providers to ensure system fonts are found
            svg.Settings.TypefaceProviders ??= new List<Svg.Skia.TypefaceProviders.ITypefaceProvider>();
            svg.Settings.TypefaceProviders.Insert(0, new SystemFontTypefaceProvider());

            using var svgStream = new MemoryStream(Encoding.UTF8.GetBytes(svgContent));

            if (svg.Load(svgStream) is { } picture)
            {
                var pngStream = new MemoryStream();
                
                // Get the bounds of the SVG
                var bounds = picture.CullRect;
                var width = (int)Math.Ceiling(bounds.Width);
                var height = (int)Math.Ceiling(bounds.Height);
                
                if (width <= 0 || height <= 0)
                {
                    // Use default size if bounds are invalid
                    width = 800;
                    height = 600;
                }
                
                // Create bitmap and canvas
                using var bitmap = new SKBitmap(width, height);
                using var canvas = new SKCanvas(bitmap);
                
                // Clear with white background
                canvas.Clear(SKColors.White);
                
                // Draw the SVG
                canvas.DrawPicture(picture);
                
                // Encode to PNG
                using var image = SKImage.FromBitmap(bitmap);
                using var data = image.Encode(SKEncodedImageFormat.Png, 100);
                data.SaveTo(pngStream);
                
                pngStream.Position = 0;
                return pngStream;
            }
        }
        catch
        {
            // If conversion fails, just skip the PNG
        }
        
        return null;
    }

    static void NormalizeFloatingPoint(StringBuilder sb)
    {
        var content = sb.ToString();
        var normalized = FloatRegex().Replace(content, match =>
        {
            var value = double.Parse(match.Value, System.Globalization.CultureInfo.InvariantCulture);
            var rounded = Math.Round(value, 4);
            return rounded.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture);
        });
        sb.Clear();
        sb.Append(normalized);
    }

    [GeneratedRegex(@"-?\d+\.\d{5,}")]
    private static partial Regex FloatRegex();
}

/// <summary>
/// Custom typeface provider that uses SKFontManager.Default to resolve system fonts.
/// </summary>
public class SystemFontTypefaceProvider : Svg.Skia.TypefaceProviders.ITypefaceProvider
{
    private static readonly SKFontManager s_fontManager = SKFontManager.Default;

    public SKTypeface? FromFamilyName(string fontFamily, SKFontStyleWeight fontWeight, SKFontStyleWidth fontWidth, SKFontStyleSlant fontStyle)
    {
        // Try each font in the comma-separated list
        var families = fontFamily.Split(',');
        foreach (var family in families)
        {
            var trimmed = family.Trim().Trim('"', '\'');
            if (string.IsNullOrEmpty(trimmed))
                continue;

            // Handle generic font families
            var familyName = trimmed.ToLowerInvariant() switch
            {
                "sans-serif" => "Arial",
                "serif" => "Times New Roman",
                "monospace" => "Consolas",
                _ => trimmed
            };

            var typeface = s_fontManager.MatchFamily(familyName, new SKFontStyle(fontWeight, fontWidth, fontStyle));
            if (typeface != null && !string.IsNullOrEmpty(typeface.FamilyName))
            {
                return typeface;
            }
        }

        // Fallback to Arial if nothing else works
        return s_fontManager.MatchFamily("Arial", new SKFontStyle(fontWeight, fontWidth, fontStyle));
    }
}