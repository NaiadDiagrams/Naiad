public class TestBase :
    PageTest
{
    // Set device scale factor for higher resolution screenshots
    public override BrowserNewContextOptions ContextOptions() =>
        new()
        {
            DeviceScaleFactor = 2
        };

    // Icon pack registration is frozen after the first render and is process-global,
    // so reset it before each test to keep tests isolated.
    [SetUp]
    public void ResetIconPacks() => IconPackRegistry.Reset();

    public async Task VerifySvg(
        string input,
        [CallerFilePath] string sourceFile = "",
        [CallerMemberName] string testMethod = "")
    {
        var svg = Mermaid.Render(input);
        svg = PrettyPrint(svg);
        var png = await GetOrCreatePngAsync(svg, sourceFile, testMethod);
        await Verify(
                svg,
                extension: "svg",
                sourceFile: sourceFile)
            .AppendFile(png, "png");
    }

    async Task<Stream> GetOrCreatePngAsync(string svg, string sourceFile, string testMethod)
    {
        var directory = Path.GetDirectoryName(sourceFile)!;
        var prefix = $"{GetType().Name}.{testMethod}.verified";
        var verifiedSvg = Path.Combine(directory, $"{prefix}.svg");
        var verifiedPng = Path.Combine(directory, $"{prefix}.png");

        if (File.Exists(verifiedSvg) &&
            File.Exists(verifiedPng) &&
            (await File.ReadAllTextAsync(verifiedSvg)).ReplaceLineEndings("\n") == svg.ReplaceLineEndings("\n"))
        {
            return File.OpenRead(verifiedPng);
        }

        return await ConvertSvgToPngAsync(svg);
    }

    static string PrettyPrint(string svg)
    {
        var doc = XDocument.Parse(svg);
        var settings = new XmlWriterSettings
        {
            Indent = true,
            NewLineOnAttributes = true,
            OmitXmlDeclaration = true
        };
        var sb = new StringBuilder();
        using (var writer = XmlWriter.Create(sb, settings))
        {
            doc.Save(writer);
        }
        return sb.ToString();
    }

    async Task<MemoryStream> ConvertSvgToPngAsync(string svgContent)
    {
        // Create an HTML page with the SVG
        var html =
            $$"""
              <!DOCTYPE html>
              <html>
              <head>
                  <meta charset="UTF-8">
                  <style>
                      * { margin: 0; padding: 0; }
                      body { background: white; display: inline-block; }
                  </style>
                  <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.7.2/css/all.min.css">
              </head>
              <body>
              {{svgContent}}
              </body>
              </html>
              """;

        await Page.SetContentAsync(
            html,
            new()
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });

        // Get the SVG element and take a screenshot
        var svg = await Page.QuerySelectorAsync("svg");
        var screenshot = await svg!.ScreenshotAsync(new()
        {
            Type = ScreenshotType.Png,
            OmitBackground = false
        });

        return new(screenshot);
    }
}