using Naiad.ImageSharp;
using Naiad.Skia;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

/// <summary>
/// Visual-regression coverage for the two PNG backends. Both drive the same shared
/// <c>SvgRasterizer</c> pipeline, so a representative spread of diagrams — exercising arcs, Béziers,
/// markers, foreignObject labels, dashes, transforms, text anchoring and the various shape primitives —
/// pins the output of the walker and both surfaces. PNGs are compared with the same fuzzy image
/// comparer the SVG suite uses (registered in Release).
/// </summary>
public class PngRenderTests
{
    // Arcs, fills, slice labels (text-anchor middle), legend swatches, title.
    const string pie = "pie title Pets\n \"Dogs\" : 40\n \"Cats\" : 30\n \"Birds\" : 20";

    // H/V node paths, foreignObject labels, arrowhead markers, group transforms.
    const string flowchart = "flowchart LR\n A[Start] --> B[Process] --> C[End]";

    // Rounded actor boxes, dashed lifelines and return arrow, markers both directions.
    const string sequence = "sequenceDiagram\n Alice->>John: Hello John\n John-->>Alice: Hi Alice";

    // Bold text, compartment lines, inheritance triangle polygon.
    const string @class = "classDiagram\n    class Animal {\n        +String name\n        +int age\n        +makeSound() void\n    }\n    Animal <|-- Dog";

    // Composite nodes, curved edges, start/end markers.
    const string state = "stateDiagram-v2\n    [*] --> Still\n    Still --> Moving\n    Moving --> Still\n    Moving --> Crash\n    Crash --> [*]";

    // Commit circles and colored branch lines.
    const string gitGraph = "gitGraph\n    commit\n    commit\n    commit";

    [Test]
    public Task SkiaPie() => VerifyPng(SkiaRenderer.RenderPng(pie, HighDpi));

    [Test]
    public Task SkiaFlowchart() => VerifyPng(SkiaRenderer.RenderPng(flowchart, HighDpi));

    [Test]
    public Task SkiaSequence() => VerifyPng(SkiaRenderer.RenderPng(sequence, HighDpi));

    [Test]
    public Task SkiaClass() => VerifyPng(SkiaRenderer.RenderPng(@class, HighDpi));

    [Test]
    public Task SkiaState() => VerifyPng(SkiaRenderer.RenderPng(state, HighDpi));

    [Test]
    public Task SkiaGitGraph() => VerifyPng(SkiaRenderer.RenderPng(gitGraph, HighDpi));

    [Test]
    public Task ImageSharpPie() => VerifyPng(ImageSharpRenderer.RenderPng(pie, HighDpi));

    [Test]
    public Task ImageSharpFlowchart() => VerifyPng(ImageSharpRenderer.RenderPng(flowchart, HighDpi));

    [Test]
    public Task ImageSharpSequence() => VerifyPng(ImageSharpRenderer.RenderPng(sequence, HighDpi));

    [Test]
    public Task ImageSharpClass() => VerifyPng(ImageSharpRenderer.RenderPng(@class, HighDpi));

    [Test]
    public Task ImageSharpState() => VerifyPng(ImageSharpRenderer.RenderPng(state, HighDpi));

    [Test]
    public Task ImageSharpGitGraph() => VerifyPng(ImageSharpRenderer.RenderPng(gitGraph, HighDpi));

    [Test]
    public async Task ScaleEnlargesOutput()
    {
        const string source = "flowchart LR\n A[Start] --> B[End]";
        using var single = Image.Load<Rgba32>(SkiaRenderer.RenderPng(source));
        using var doubled = Image.Load<Rgba32>(SkiaRenderer.RenderPng(source, new() {Png = {Scale = 2}}));
        await Assert.That(doubled.Width).IsEqualTo(single.Width * 2).Within(2);
        await Assert.That(doubled.Height).IsEqualTo(single.Height * 2).Within(2);
    }

    // Render at 2x to match the device-pixel scale the SVG snapshot suite uses.
    static RenderOptions HighDpi => new() {Png = {Scale = 2}};

    static Task VerifyPng(byte[] png, [CallerFilePath] string sourceFile = "") =>
        Verify(new MemoryStream(png), extension: "png", sourceFile: sourceFile);
}
