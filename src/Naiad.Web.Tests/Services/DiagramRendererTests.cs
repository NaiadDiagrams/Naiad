public class DiagramRendererTests
{
    [Test]
    public async Task Render_ValidSource_ReturnsSvg()
    {
        var result = DiagramRenderer.Render("flowchart LR\n    A --> B");

        await Assert.That(result.HasSvg).IsTrue();
        await Assert.That(result.HasError).IsFalse();
        await Assert.That(result.IsUnexpected).IsFalse();
        await Assert.That(result.Svg).Contains("<svg");
    }

    [Test]
    public async Task Render_WhitespaceSource_IsEmpty()
    {
        var result = DiagramRenderer.Render("   \n  ");

        await Assert.That(result.HasSvg).IsFalse();
        await Assert.That(result.HasError).IsFalse();
    }

    [Test]
    public async Task Render_UnknownDiagram_ReturnsExpectedError()
    {
        var result = DiagramRenderer.Render("notADiagram foo");

        await Assert.That(result.HasError).IsTrue();
        await Assert.That(result.HasSvg).IsFalse();
        // A parse / unknown-type failure is user error, not a bug — no issue prompt.
        await Assert.That(result.IsUnexpected).IsFalse();
    }

    [Test]
    public async Task RenderForPng_ProducesSelfContainedSvg()
    {
        // The PNG path disables HTML elements so the markup a browser canvas rasterizes is self-contained:
        // native <text> rather than <foreignObject>, and no Font Awesome @import.
        var svg = DiagramRenderer.RenderForPng("flowchart LR\n    A[Start] --> B[End]");

        await Assert.That(svg).Contains("<svg");
        await Assert.That(svg).DoesNotContain("foreignObject");
        await Assert.That(svg).DoesNotContain("@import");
    }
}
