public class TestBase
{
    // Icon pack registration is frozen after the first render and is process-global,
    // so reset it before each test to keep tests isolated.
    [Before(Test)]
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

        return new MemoryStream(SvgRenderer.RenderToPng(svg));
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
}