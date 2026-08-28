public class TestBase
{
    // Icon pack registration is frozen after the first render and is process-global,
    // so reset it before each test to keep tests isolated.
    [Before(Test)]
    public void ResetIconPacks() => IconPackRegistry.Reset();

    public static Task VerifySvg(
        string input,
        [CallerFilePath] string sourceFile = "",
        [CallerMemberName] string testMethod = "") =>
        VerifySvg(input, null, sourceFile, testMethod);

    public static async Task VerifySvg(
        string input,
        RenderOptions? options,
        [CallerFilePath] string sourceFile = "",
        [CallerMemberName] string testMethod = "")
    {
        var svg = options is null ? Mermaid.Render(input) : Mermaid.Render(input, options);
        svg = PrettyPrint(svg);

        // Always rasterize. Reusing the committed .verified.png whenever the SVG was unchanged handed Verify
        // that file as the received value and so compared it against itself: the PNG leg passed whatever the
        // file held. That is backwards, because a rasterization regression is precisely the case where the
        // SVG does not change. PNGs are compared by SSIM (see ModuleInitializer), so incidental
        // anti-aliasing differences still pass.
        var png = new MemoryStream(SvgRenderer.RenderToPng(svg));

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
}