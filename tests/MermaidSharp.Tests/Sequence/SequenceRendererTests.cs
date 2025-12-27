using TUnit.Core;

namespace MermaidSharp.Tests.Sequence;

public class SequenceRendererTests
{
    [Test]
    public Task Render_SimpleSequence()
    {
        const string input = """
            sequenceDiagram
                Alice->>Bob: Hello Bob
                Bob-->>Alice: Hi Alice
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg);
    }

    [Test]
    public Task Render_SequenceWithParticipants()
    {
        const string input = """
            sequenceDiagram
                participant A as Alice
                participant B as Bob
                A->>B: Hello
                B->>A: Hi
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg);
    }

    [Test]
    public Task Render_SequenceWithActors()
    {
        const string input = """
            sequenceDiagram
                actor User
                participant Server
                User->>Server: Request
                Server-->>User: Response
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg);
    }

    [Test]
    public Task Render_SequenceWithActivation()
    {
        const string input = """
            sequenceDiagram
                Alice->>+Bob: Hello
                Bob-->>-Alice: Hi
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg);
    }

    [Test]
    public Task Render_SequenceWithNotes()
    {
        const string input = """
            sequenceDiagram
                Alice->>Bob: Hello
                Note right of Bob: Bob thinks
                Bob-->>Alice: Hi
                Note over Alice,Bob: Conversation
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg);
    }

    [Test]
    public Task Render_SequenceWithAutoNumber()
    {
        const string input = """
            sequenceDiagram
                autonumber
                Alice->>Bob: Hello
                Bob->>Alice: Hi
                Alice->>Bob: How are you?
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg);
    }

    [Test]
    public Task Render_SequenceWithDifferentArrows()
    {
        const string input = """
            sequenceDiagram
                A->>B: Solid arrow
                A-->>B: Dotted arrow
                A->B: Solid open
                A-->B: Dotted open
                A-xB: Solid cross
                A--xB: Dotted cross
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg);
    }

    [Test]
    public Task Render_SequenceWithTitle()
    {
        const string input = """
            sequenceDiagram
                title Authentication Flow
                Client->>Server: Login request
                Server-->>Client: Token
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg);
    }
}
