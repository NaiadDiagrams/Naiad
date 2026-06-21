public class IndexTests : BunitTestContext
{
    public IndexTests() =>
        JSInterop.Mode = JSRuntimeMode.Loose;

    [Test]
    public async Task InitialRender_RendersDefaultSampleSvg()
    {
        var cut = Render<Naiad.Web.Pages.Index>();

        // The default sample renders straight away, so the preview holds an SVG on first paint.
        await Assert.That(cut.FindAll(".preview-content svg")).IsNotEmpty();
        await Assert.That(cut.FindAll(".preview-empty")).IsEmpty();
    }

    [Test]
    public async Task InitialRender_EditorHoldsDefaultSource()
    {
        var cut = Render<Naiad.Web.Pages.Index>();

        // The default flowchart sample is bound into the editor, so its source appears in the rendered
        // markup. "flowchart LR" is unique to that source — the preview SVG only carries the hyphenated
        // "flowchart-link" class, never the bare "flowchart LR" header.
        await Assert.That(cut.Markup).Contains("flowchart LR");
    }

    [Test]
    public async Task InitialRender_LinksToDetectedTypeDocs()
    {
        var cut = Render<Naiad.Web.Pages.Index>();

        // The default sample is a flowchart, so the editor deep-links to the flowchart syntax reference.
        var link = cut.Find(".doc-link");
        await Assert.That(link.GetAttribute("href")).IsEqualTo("https://mermaid.js.org/syntax/flowchart.html");
        await Assert.That(link.TextContent).Contains("Flowchart");
    }

    [Test]
    public async Task InitialRender_HasTwoEnabledDownloadButtons()
    {
        var cut = Render<Naiad.Web.Pages.Index>();

        var buttons = cut.FindAll(".toolbar-btn");
        await Assert.That(buttons.Count).IsEqualTo(2);
        // A valid default preview means both SVG and PNG downloads are available.
        await Assert.That(buttons.Any(_ => _.HasAttribute("disabled"))).IsFalse();
    }
}
