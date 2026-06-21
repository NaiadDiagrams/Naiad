public class MermaidTests
{
    [Test]
    public async Task TryDetectType_KnownKeywords_ResolveToType()
    {
        await Assert.That(Detect("flowchart LR\n    A --> B")).IsEqualTo(DiagramType.Flowchart);
        await Assert.That(Detect("graph TD\n    A --> B")).IsEqualTo(DiagramType.Flowchart);
        await Assert.That(Detect("sequenceDiagram\n    A->>B: hi")).IsEqualTo(DiagramType.Sequence);
        // -v2 / -beta suffixes ride on the same StartsWith prefix the renderer matches.
        await Assert.That(Detect("stateDiagram-v2\n    [*] --> Idle")).IsEqualTo(DiagramType.State);
        await Assert.That(Detect("sankey-beta\n    A,B,1")).IsEqualTo(DiagramType.Sankey);

        static DiagramType Detect(string input)
        {
            if (Mermaid.TryDetectType(input, out var type))
            {
                return type;
            }

            throw new($"No diagram type detected for: {input}");
        }
    }

    [Test]
    public async Task TryDetectType_SkipsLeadingInitBlock()
    {
        var detected = Mermaid.TryDetectType(
            """
            %%{init: {"theme": "dark"}}%%
            flowchart TD
                A --> B
            """,
            out var type);

        await Assert.That(detected).IsTrue();
        await Assert.That(type).IsEqualTo(DiagramType.Flowchart);
    }

    [Test]
    public async Task TryDetectType_EmptyOrUnknown_ReturnsFalse()
    {
        await Assert.That(Mermaid.TryDetectType(null, out _)).IsFalse();
        await Assert.That(Mermaid.TryDetectType("   ", out _)).IsFalse();
        await Assert.That(Mermaid.TryDetectType("notADiagram foo", out _)).IsFalse();
    }
}
