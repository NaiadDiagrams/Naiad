using Naiad.ImageSharp;
using Naiad.Skia;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Tests.Rendering;

/// <summary>
/// Visual-regression coverage for the two PNG backends. Both drive the same shared
/// <c>SvgRasterizer</c> pipeline, so a representative spread of diagrams — exercising arcs, Béziers,
/// markers, foreignObject labels, dashes, transforms, text anchoring and the various shape primitives —
/// pins the output of the walker and both surfaces. PNGs are compared with the same fuzzy image
/// comparer the SVG suite uses (registered in Release).
/// </summary>
[TestFixture]
public class PngRenderTests
{
    static readonly Dictionary<string, string> samples = new()
    {
        // Arcs, fills, slice labels (text-anchor middle), legend swatches, title.
        ["pie"] = "pie title Pets\n \"Dogs\" : 40\n \"Cats\" : 30\n \"Birds\" : 20",
        // H/V node paths, foreignObject labels, arrowhead markers, group transforms.
        ["flowchart"] = "flowchart LR\n A[Start] --> B[Process] --> C[End]",
        // Rounded actor boxes, dashed lifelines and return arrow, markers both directions.
        ["sequence"] = "sequenceDiagram\n Alice->>John: Hello John\n John-->>Alice: Hi Alice",
        // Bold text, compartment lines, inheritance triangle polygon.
        ["class"] = "classDiagram\n    class Animal {\n        +String name\n        +int age\n        +makeSound() void\n    }\n    Animal <|-- Dog",
        // Composite nodes, curved edges, start/end markers.
        ["state"] = "stateDiagram-v2\n    [*] --> Still\n    Still --> Moving\n    Moving --> Still\n    Moving --> Crash\n    Crash --> [*]",
        // Commit circles and colored branch lines.
        ["gitgraph"] = "gitGraph\n    commit\n    commit\n    commit",
    };

    public static IEnumerable<string> Names => samples.Keys;

    [Test]
    [TestCaseSource(nameof(Names))]
    public Task Skia(string name) =>
        VerifyPng(SkiaRenderer.RenderPng(samples[name], HighDpi), name);

    [Test]
    [TestCaseSource(nameof(Names))]
    public Task ImageSharp(string name) =>
        VerifyPng(ImageSharpRenderer.RenderPng(samples[name], HighDpi), name);

    [Test]
    public void ScaleEnlargesOutput()
    {
        const string source = "flowchart LR\n A[Start] --> B[End]";
        using var single = Image.Load<Rgba32>(SkiaRenderer.RenderPng(source));
        using var doubled = Image.Load<Rgba32>(SkiaRenderer.RenderPng(source, new() {Png = {Scale = 2}}));
        Assert.That(doubled.Width, Is.EqualTo(single.Width * 2).Within(2));
        Assert.That(doubled.Height, Is.EqualTo(single.Height * 2).Within(2));
    }

    // Render at 2x to match the device-pixel scale the SVG snapshot suite uses.
    static RenderOptions HighDpi => new() {Png = {Scale = 2}};

    static Task VerifyPng(byte[] png, string name, [CallerFilePath] string sourceFile = "") =>
        Verify(new MemoryStream(png), extension: "png", sourceFile: sourceFile).UseParameters(name);
}
