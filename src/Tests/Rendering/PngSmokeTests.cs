using Naiad.ImageSharp;
using Naiad.Skia;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Tests.Rendering;

[TestFixture]
public class PngSmokeTests
{
    static readonly (string Name, string Source)[] samples =
    [
        ("pie", "pie title Pets\n \"Dogs\" : 40\n \"Cats\" : 30\n \"Birds\" : 20"),
        ("flowchart", "flowchart LR\n A[Start] --> B[Process] --> C[End]"),
        ("sequence", "sequenceDiagram\n Alice->>John: Hello John\n John-->>Alice: Hi Alice"),
        ("class", "classDiagram\n    class Animal {\n        +String name\n        +int age\n        +makeSound() void\n    }\n    Animal <|-- Dog"),
    ];

    public static IEnumerable<string> SampleNames => samples.Select(_ => _.Name);

    [Test]
    [TestCaseSource(nameof(SampleNames))]
    public void Skia(string name)
    {
        var source = samples.First(_ => _.Name == name).Source;
        AssertValidContentfulPng(SkiaRenderer.RenderPng(source));
    }

    [Test]
    [TestCaseSource(nameof(SampleNames))]
    public void ImageSharp(string name)
    {
        var source = samples.First(_ => _.Name == name).Source;
        AssertValidContentfulPng(ImageSharpRenderer.RenderPng(source));
    }

    [Test]
    public void ScaleEnlargesOutput()
    {
        const string source = "flowchart LR\n A[Start] --> B[End]";
        using var single = Image.Load<Rgba32>(SkiaRenderer.RenderPng(source));
        using var doubled = Image.Load<Rgba32>(SkiaRenderer.RenderPng(source, new() {Png = {Scale = 2}}));
        Assert.That(doubled.Width, Is.EqualTo(single.Width * 2).Within(2));
        Assert.That(doubled.Height, Is.EqualTo(single.Height * 2).Within(2));
    }

    [Test]
    [Explicit]
    public void DumpPreviews()
    {
        var directory = Path.Combine(Path.GetTempPath(), "naiad-png-preview");
        Directory.CreateDirectory(directory);
        foreach (var (name, source) in samples)
        {
            SkiaRenderer.RenderPng(source, Path.Combine(directory, $"{name}.skia.png"), new() {Png = {Scale = 2}});
            ImageSharpRenderer.RenderPng(source, Path.Combine(directory, $"{name}.imagesharp.png"), new() {Png = {Scale = 2}});
        }

        TestContext.Out.WriteLine(directory);
    }

    static void AssertValidContentfulPng(byte[] bytes)
    {
        Assert.That(bytes, Is.Not.Empty);
        // PNG signature.
        Assert.That(bytes[..4], Is.EqualTo(new byte[] {0x89, 0x50, 0x4E, 0x47}));

        using var image = Image.Load<Rgba32>(bytes);
        Assert.That(image.Width, Is.GreaterThan(10));
        Assert.That(image.Height, Is.GreaterThan(10));

        var nonBackground = 0;
        image.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < accessor.Height; y++)
            {
                foreach (ref var pixel in accessor.GetRowSpan(y))
                {
                    if (pixel is not {R: 255, G: 255, B: 255})
                    {
                        nonBackground++;
                    }
                }
            }
        });

        // A rendered diagram should mark a meaningful number of pixels (strokes, fills, glyphs).
        Assert.That(nonBackground, Is.GreaterThan(500), "PNG appears blank — nothing was drawn.");
    }
}
