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
}
