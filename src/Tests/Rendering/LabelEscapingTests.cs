// Regression coverage for label escaping: user-supplied diagram text must never reach the rendered SVG as
// live markup. Flowchart node/edge labels travel through the <foreignObject> seam (SvgForeignObject emits
// raw XHTML, so the renderer pre-encodes the text), while other diagrams' labels go through native <text>
// (escaped by SvgText). A crafted label must come out inert either way.
public class LabelEscapingTests
{
    [Test]
    public async Task FlowchartNodeLabel_EscapesMarkup()
    {
        var svg = Mermaid.Render(
            """
            flowchart LR
                A["<script>alert(1)</script>"]
            """);

        await Assert.That(svg).DoesNotContain("<script>");
        await Assert.That(svg).Contains("&lt;script&gt;");
    }

    [Test]
    public async Task FlowchartNodeLabel_EscapesAttributeInjection()
    {
        var svg = Mermaid.Render(
            """
            flowchart LR
                A["<img src=x onerror=alert(1)>"]
            """);

        // The raw <img> tag must not appear; it comes out as encoded text instead.
        await Assert.That(svg).DoesNotContain("<img");
        await Assert.That(svg).Contains("&lt;img");
    }

    [Test]
    public async Task FlowchartEdgeLabel_EscapesMarkup()
    {
        var svg = Mermaid.Render(
            """
            flowchart LR
                A -->|"<script>x</script>"| B
            """);

        await Assert.That(svg).DoesNotContain("<script>");
        await Assert.That(svg).Contains("&lt;script&gt;");
    }

    // Locks the seam contract: SvgForeignObject emits HtmlContent verbatim (callers own escaping). Guards
    // against a well-meaning "fix" that blanket-encodes here — which would corrupt the <p>/<i> markup the
    // renderers legitimately pass.
    [Test]
    public async Task SvgForeignObject_EmitsHtmlContentVerbatim()
    {
        var foreignObject = new SvgForeignObject
        {
            X = 0,
            Y = 0,
            Width = 10,
            Height = 10,
            HtmlContent = "<p>kept &amp; intact</p>"
        };

        var builder = new StringBuilder();
        foreignObject.ToXml(builder);

        await Assert.That(builder.ToString()).Contains("<p>kept &amp; intact</p>");
    }
}
