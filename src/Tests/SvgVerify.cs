public class TestBase :
    PageTest
{
    // Set device scale factor for higher resolution screenshots
    public override BrowserNewContextOptions ContextOptions() =>
        new()
        {
            DeviceScaleFactor = 2
        };

    public async Task VerifySvg(
        string input,
        [CallerFilePath] string sourceFile = "")
    {
        var svg = Mermaid.Render(input);
        svg = PrettyPrint(svg);
        var png = await ConvertSvgToPngAsync(svg);
        await Verify(
                svg,
                extension: "svg",
                sourceFile: sourceFile)
            .AppendFile(png, "png");
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