public static partial class ModuleInitializer
{
    private static IPlaywright? _playwright;
    private static IBrowser? _browser;
    private static readonly SemaphoreSlim _browserLock = new(1, 1);

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

                // Convert to PNG using Playwright (browser-based rendering)
                var pngStream = ConvertSvgToPngAsync(svgContent).GetAwaiter().GetResult();
                if (pngStream != null)
                {
                    targets.Add(new Target("png", pngStream));
                }

                return new ConversionResult(null, targets);
            });
    }

    static async Task<MemoryStream?> ConvertSvgToPngAsync(string svgContent)
    {
        try
        {
            var page = await GetBrowserPageAsync();

            // Create an HTML page with the SVG
            var html = $@"<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <style>
        * {{ margin: 0; padding: 0; }}
        body {{ background: white; display: inline-block; }}
    </style>
    <link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.7.2/css/all.min.css"">
</head>
<body>
{svgContent}
</body>
</html>";

            await page.SetContentAsync(html, new() { WaitUntil = WaitUntilState.NetworkIdle });

            // Get the SVG element and take a screenshot
            var svg = await page.QuerySelectorAsync("svg");
            if (svg == null) return null;

            var screenshot = await svg.ScreenshotAsync(new()
            {
                Type = ScreenshotType.Png,
                OmitBackground = false
            });

            return new MemoryStream(screenshot);
        }
        catch
        {
            // If conversion fails, skip the PNG
            return null;
        }
    }

    static async Task<IPage> GetBrowserPageAsync()
    {
        await _browserLock.WaitAsync();
        try
        {
            if (_playwright == null)
            {
                _playwright = await Playwright.CreateAsync();
                _browser = await _playwright.Chromium.LaunchAsync(new()
                {
                    Headless = true
                });
            }

            return await _browser!.NewPageAsync();
        }
        finally
        {
            _browserLock.Release();
        }
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
