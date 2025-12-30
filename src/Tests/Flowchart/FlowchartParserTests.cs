using MermaidSharp.Models;
using MermaidSharp.Diagrams.Flowchart;

public class FlowchartParserTests
{
    [Test]
    public void Simple_ReturnsNodes()
    {
        const string input =
            """
            flowchart LR
                A[Start] --> B[End]
            """;

        var parser = new FlowchartParser();
        var result = parser.Parse(input);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Value.Nodes.Count, Is.EqualTo(2));
        Assert.That(result.Value.Edges.Count, Is.EqualTo(1));
    }

    [Test]
    public void Direction_ParsesDirection()
    {
        const string input =
            """
            flowchart TD
                A --> B
            """;

        var parser = new FlowchartParser();
        var result = parser.Parse(input);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Value.Direction, Is.EqualTo(Direction.TopToBottom));
    }

    [Test]
    public void RoundedNodes_ParsesShape()
    {
        const string input =
            """
            flowchart LR
                A(Rounded)
            """;

        var parser = new FlowchartParser();
        var result = parser.Parse(input);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Value.Nodes[0].Shape, Is.EqualTo(NodeShape.RoundedRectangle));
        Assert.That(result.Value.Nodes[0].Label, Is.EqualTo("Rounded"));
    }

    [Test]
    public void Diamond_ParsesShape()
    {
        const string input =
            """
            flowchart LR
                A{Decision}
            """;

        var parser = new FlowchartParser();
        var result = parser.Parse(input);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Value.Nodes[0].Shape, Is.EqualTo(NodeShape.Diamond));
    }

    [Test]
    public void Circle_ParsesShape()
    {
        const string input =
            """
            flowchart LR
                A((Circle))
            """;

        var parser = new FlowchartParser();
        var result = parser.Parse(input);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Value.Nodes[0].Shape, Is.EqualTo(NodeShape.Circle));
    }

    [Test]
    public void ChainedNodes_CreatesMultipleEdges()
    {
        const string input =
            """
            flowchart LR
                A --> B --> C --> D
            """;

        var parser = new FlowchartParser();
        var result = parser.Parse(input);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Value.Nodes.Count, Is.EqualTo(4));
        Assert.That(result.Value.Edges.Count, Is.EqualTo(3));
    }

    [Test]
    public void DottedArrow_ParsesEdgeStyle()
    {
        const string input =
            """
            flowchart LR
                A -.-> B
            """;

        var parser = new FlowchartParser();
        var result = parser.Parse(input);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Value.Edges[0].LineStyle, Is.EqualTo(EdgeStyle.Dotted));
    }

    [Test]
    public void ThickArrow_ParsesEdgeStyle()
    {
        const string input =
            """
            flowchart LR
                A ==> B
            """;

        var parser = new FlowchartParser();
        var result = parser.Parse(input);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Value.Edges[0].LineStyle, Is.EqualTo(EdgeStyle.Thick));
    }
}
