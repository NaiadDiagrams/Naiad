public class FlowchartTests : TestBase
{
    [Test]
    public Task Simple()
    {
        const string input =
            """
            flowchart LR
                A[Start] --> B[Process] --> C[End]
            """;

        return VerifySvg(input);
    }

    [Test]
    public Task Complex()
    {
        const string input =
            """
            flowchart TD
                A[Christmas] -->|Get money| B(Go shopping)
                B --> C{Let me think}
                C -->|One| D[Laptop]
                C -->|Two| E[iPhone]
                C -->|Three| F[fa:fa-car Car]
            """;

        return VerifySvg(input);
    }

    [Test]
    public Task IconPackIcon()
    {
        const string input =
            """
            flowchart LR
                A[sample:box Storage] --> B[sample:ring Cache]
            """;

        // A registered iconify pack icon (prefix:name) renders inline, like FontAwesome.
        const string pack =
            """
            {
              "prefix": "sample",
              "width": 24,
              "height": 24,
              "icons": {
                "box": {"body": "<rect x=\"3\" y=\"3\" width=\"18\" height=\"18\" rx=\"3\" fill=\"currentColor\"/>"},
                "ring": {"body": "<circle cx=\"12\" cy=\"12\" r=\"8\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\"/>"}
              }
            }
            """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(pack));
        IconPack.Load(stream);

        return VerifySvg(input);
    }

    [Test]
    public Task Shapes()
    {
        const string input =
            """
            flowchart TD
                A[Rectangle]
                B(Rounded)
                C{Diamond}
                D((Circle))
            """;

        return VerifySvg(input);
    }

    [Test]
    public Task EdgeLabels()
    {
        const string input =
            """
            flowchart LR
                A --> |Yes| B
                A --> |No| C
            """;

        return VerifySvg(input);
    }

    [Test]
    public Task GraphKeyword()
    {
        const string input =
            """
            graph TD
                A --> B --> C
            """;

        return VerifySvg(input);
    }

    [Test]
    public void LeadingAndTrailingWhitespace()
    {
        const string input =
            """
            flowchart LR
                A[Start] --> B[Process] --> C[End]
            """;

        var expected = Mermaid.Render(input);
        var actual = Mermaid.Render("\r\n\r\n" + input + "\r\n\r\n");

        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public Task Subgraphs()
    {
        const string input =
            """
            flowchart TB
                Start[Start] --> A

                subgraph frontend [Frontend]
                    A[Web UI] --> B[Mobile UI]
                end

                subgraph backend [Backend]
                    C[API] --> D[(Database)]
                end

                A --> C
                B --> C
            """;

        return VerifySvg(input);
    }

    [Test]
    public Task NestedSubgraphs()
    {
        const string input =
            """
            flowchart TB
                User[User] --> A

                subgraph system [Banking System]
                    subgraph api [API Application]
                        A[Controller] --> B[Service]
                    end
                    B --> C[(Database)]
                end
            """;

        return VerifySvg(input);
    }
}
