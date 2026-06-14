/// <summary>
/// Regenerates every *.verified.png baseline from the committed *.verified.svg
/// using <see cref="SvgRenderer"/> (Svg.Skia). Run manually after changing the
/// rasterization path. The .verified.svg files are the source of truth and are
/// left untouched.
/// </summary>
public class PngRegenerator
{
    [Test]
    [Explicit("Run manually to regenerate all verified PNG baselines from the verified SVGs")]
    public void RegenerateAllVerifiedPngs()
    {
        var svgFiles = Directory.GetFiles(
            ProjectFiles.ProjectDirectory,
            "*.verified.svg",
            SearchOption.AllDirectories);

        var failures = new List<string>();

        foreach (var svgFile in svgFiles)
        {
            try
            {
                var svg = File.ReadAllText(svgFile);
                var png = SvgRenderer.RenderToPng(svg);
                File.WriteAllBytes(Path.ChangeExtension(svgFile, ".png"), png);
            }
            catch (Exception exception)
            {
                failures.Add($"{Path.GetFileName(svgFile)}: {exception.Message}");
            }
        }

        TestContext.Out.WriteLine($"Regenerated {svgFiles.Length - failures.Count}/{svgFiles.Length} PNG baselines.");

        if (failures.Count > 0)
        {
            throw new($"Failed to render {failures.Count} SVG(s):\n{string.Join("\n", failures)}");
        }
    }
}
