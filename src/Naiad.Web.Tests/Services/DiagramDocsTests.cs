public class DiagramDocsTests
{
    // Every advertised sample must resolve to a real Mermaid syntax page — this keeps the editor's docs
    // link honest as samples and detection evolve. A failure names the offending sample and its URL.
    [Test]
    public async Task For_EverySample_LinksToMermaidSyntaxPage()
    {
        var failures = new List<string>();

        foreach (var sample in DiagramSamples.All)
        {
            var link = DiagramDocs.For(sample.Source);
            if (link is null)
            {
                failures.Add($"{sample.Name}: no docs link");
            }
            else if (!IsMermaidSyntaxUrl(link.Url))
            {
                failures.Add($"{sample.Name}: unexpected url {link.Url}");
            }
        }

        await Assert.That(string.Join("; ", failures)).IsEqualTo("");
    }

    // Guards the map's coverage: a newly added DiagramType that nobody mapped would fall through to the
    // bare base URL (no .html page) and be caught here.
    [Test]
    public async Task For_EveryDiagramType_HasSyntaxPage()
    {
        var failures = new List<string>();

        foreach (var type in Enum.GetValues<DiagramType>())
        {
            var link = DiagramDocs.For(type);
            if (string.IsNullOrEmpty(link.Name) || !IsMermaidSyntaxUrl(link.Url))
            {
                failures.Add($"{type}: {link.Name} / {link.Url}");
            }
        }

        await Assert.That(string.Join("; ", failures)).IsEqualTo("");
    }

    [Test]
    public async Task For_Flowchart_LinksToFlowchartSyntax()
    {
        var link = DiagramDocs.For("flowchart LR\n    A --> B");

        await Assert.That(link).IsNotNull();
        await Assert.That(link!.Name).IsEqualTo("Flowchart");
        await Assert.That(link.Url).IsEqualTo("https://mermaid.js.org/syntax/flowchart.html");
    }

    [Test]
    public async Task For_UnknownOrEmptySource_ReturnsNull()
    {
        await Assert.That(DiagramDocs.For("notADiagram foo")).IsNull();
        await Assert.That(DiagramDocs.For("   ")).IsNull();
        await Assert.That(DiagramDocs.For(null)).IsNull();
    }

    static bool IsMermaidSyntaxUrl(string url) =>
        url.StartsWith("https://mermaid.js.org/syntax/", StringComparison.Ordinal) &&
        url.EndsWith(".html", StringComparison.Ordinal);
}
