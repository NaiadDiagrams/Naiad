using Naiad.Models;

public class FlowchartParserTests
{
    [Test]
    public async Task Simple_ReturnsNodes()
    {
        const string input =
            """
            flowchart LR
                A[Start] --> B[End]
            """;

        var parser = new FlowchartParser();
        var result = parser.Parse(input);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Value.Nodes.Count).IsEqualTo(2);
        await Assert.That(result.Value.Edges.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Direction_ParsesDirection()
    {
        const string input =
            """
            flowchart TD
                A --> B
            """;

        var parser = new FlowchartParser();
        var result = parser.Parse(input);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Value.Direction).IsEqualTo(Direction.TopToBottom);
    }

    [Test]
    public async Task RoundedNodes_ParsesShape()
    {
        const string input =
            """
            flowchart LR
                A(Rounded)
            """;

        var parser = new FlowchartParser();
        var result = parser.Parse(input);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Value.Nodes[0].Shape).IsEqualTo(NodeShape.RoundedRectangle);
        await Assert.That(result.Value.Nodes[0].Label).IsEqualTo("Rounded");
    }

    [Test]
    public async Task Diamond_ParsesShape()
    {
        const string input =
            """
            flowchart LR
                A{Decision}
            """;

        var parser = new FlowchartParser();
        var result = parser.Parse(input);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Value.Nodes[0].Shape).IsEqualTo(NodeShape.Diamond);
    }

    [Test]
    public async Task Circle_ParsesShape()
    {
        const string input =
            """
            flowchart LR
                A((Circle))
            """;

        var parser = new FlowchartParser();
        var result = parser.Parse(input);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Value.Nodes[0].Shape).IsEqualTo(NodeShape.Circle);
    }

    [Test]
    public async Task ChainedNodes_CreatesMultipleEdges()
    {
        const string input =
            """
            flowchart LR
                A --> B --> C --> D
            """;

        var parser = new FlowchartParser();
        var result = parser.Parse(input);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Value.Nodes.Count).IsEqualTo(4);
        await Assert.That(result.Value.Edges.Count).IsEqualTo(3);
    }

    [Test]
    public async Task DottedArrow_ParsesEdgeStyle()
    {
        const string input =
            """
            flowchart LR
                A -.-> B
            """;

        var parser = new FlowchartParser();
        var result = parser.Parse(input);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Value.Edges[0].LineStyle).IsEqualTo(EdgeStyle.Dotted);
    }

    [Test]
    public async Task ThickArrow_ParsesEdgeStyle()
    {
        const string input =
            """
            flowchart LR
                A ==> B
            """;

        var parser = new FlowchartParser();
        var result = parser.Parse(input);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Value.Edges[0].LineStyle).IsEqualTo(EdgeStyle.Thick);
    }

    [Test]
    public async Task InlineEdgeLabel_ParsesLabelAndType()
    {
        const string input =
            """
            flowchart LR
                A -- yes --> B
            """;

        var parser = new FlowchartParser();
        var result = parser.Parse(input);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Value.Edges.Count).IsEqualTo(1);
        await Assert.That(result.Value.Edges[0].Type).IsEqualTo(EdgeType.Arrow);
        await Assert.That(result.Value.Edges[0].Label).IsEqualTo("yes");
    }

    [Test]
    public async Task InlineDottedLabel_ParsesLabelAndType()
    {
        const string input =
            """
            flowchart LR
                A -. cache .-> B
            """;

        var parser = new FlowchartParser();
        var result = parser.Parse(input);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Value.Edges[0].Type).IsEqualTo(EdgeType.DottedArrow);
        await Assert.That(result.Value.Edges[0].LineStyle).IsEqualTo(EdgeStyle.Dotted);
        await Assert.That(result.Value.Edges[0].Label).IsEqualTo("cache");
    }

    [Test]
    public async Task InlineThickLabel_ParsesLabelAndType()
    {
        const string input =
            """
            flowchart LR
                A == call ==> B
            """;

        var parser = new FlowchartParser();
        var result = parser.Parse(input);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Value.Edges[0].Type).IsEqualTo(EdgeType.ThickArrow);
        await Assert.That(result.Value.Edges[0].LineStyle).IsEqualTo(EdgeStyle.Thick);
        await Assert.That(result.Value.Edges[0].Label).IsEqualTo("call");
    }

    [Test]
    public async Task Parallelogram_ParsesShape()
    {
        const string input =
            """
            flowchart LR
                A[/Validate/]
            """;

        var parser = new FlowchartParser();
        var result = parser.Parse(input);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Value.Nodes[0].Shape).IsEqualTo(NodeShape.Parallelogram);
        await Assert.That(result.Value.Nodes[0].Label).IsEqualTo("Validate");
    }

    [Test]
    public async Task ClassShorthand_IsIgnoredButNodeKept()
    {
        const string input =
            """
            flowchart LR
                A[Start]:::highlight --> B
            """;

        var parser = new FlowchartParser();
        var result = parser.Parse(input);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Value.Nodes.Count).IsEqualTo(2);
        await Assert.That(result.Value.Nodes[0].Label).IsEqualTo("Start");
        await Assert.That(result.Value.Edges.Count).IsEqualTo(1);
    }

    [Test]
    public async Task DirectionInsideSubgraph_SetsDirectionWithoutTruncating()
    {
        const string input =
            """
            flowchart TB
                subgraph s [Sub]
                    direction LR
                    A --> B
                end
                B --> C
            """;

        var parser = new FlowchartParser();
        var result = parser.Parse(input);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Value.Subgraphs[0].Direction).IsEqualTo(Direction.LeftToRight);
        // The statements after `direction` (and after the subgraph) must still be parsed - no truncation.
        await Assert.That(result.Value.Nodes.Count).IsEqualTo(3);
        await Assert.That(result.Value.Edges.Count).IsEqualTo(2);
    }

    [Test]
    public async Task UnknownLine_IsSkippedNotTruncating()
    {
        const string input =
            """
            flowchart LR
                A --> B
                linkStyle default stroke:#999
                B --> C
            """;

        var parser = new FlowchartParser();
        var result = parser.Parse(input);

        // The unrecognised `linkStyle` line is skipped; the statement after it is still parsed.
        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Value.Nodes.Count).IsEqualTo(3);
        await Assert.That(result.Value.Edges.Count).IsEqualTo(2);
    }
}
