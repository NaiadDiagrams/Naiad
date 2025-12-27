using MermaidSharp.Core.Models;
using MermaidSharp.Diagrams.Flowchart;
using TUnit.Core;

namespace MermaidSharp.Tests.Flowchart;

public class FlowchartParserTests
{
    [Test]
    public async Task Parse_SimpleFlowchart_ReturnsNodes()
    {
        const string input = """
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
    public async Task Parse_FlowchartWithDirection_ParsesDirection()
    {
        const string input = """
            flowchart TD
                A --> B
            """;

        var parser = new FlowchartParser();
        var result = parser.Parse(input);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Value.Direction).IsEqualTo(Direction.TopToBottom);
    }

    [Test]
    public async Task Parse_FlowchartWithRoundedNodes_ParsesShape()
    {
        const string input = """
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
    public async Task Parse_FlowchartWithDiamond_ParsesShape()
    {
        const string input = """
            flowchart LR
                A{Decision}
            """;

        var parser = new FlowchartParser();
        var result = parser.Parse(input);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Value.Nodes[0].Shape).IsEqualTo(NodeShape.Diamond);
    }

    [Test]
    public async Task Parse_FlowchartWithCircle_ParsesShape()
    {
        const string input = """
            flowchart LR
                A((Circle))
            """;

        var parser = new FlowchartParser();
        var result = parser.Parse(input);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Value.Nodes[0].Shape).IsEqualTo(NodeShape.Circle);
    }

    [Test]
    public async Task Parse_ChainedNodes_CreatesMultipleEdges()
    {
        const string input = """
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
    public async Task Parse_DottedArrow_ParsesEdgeStyle()
    {
        const string input = """
            flowchart LR
                A -.-> B
            """;

        var parser = new FlowchartParser();
        var result = parser.Parse(input);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Value.Edges[0].LineStyle).IsEqualTo(EdgeStyle.Dotted);
    }

    [Test]
    public async Task Parse_ThickArrow_ParsesEdgeStyle()
    {
        const string input = """
            flowchart LR
                A ==> B
            """;

        var parser = new FlowchartParser();
        var result = parser.Parse(input);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Value.Edges[0].LineStyle).IsEqualTo(EdgeStyle.Thick);
    }
}
