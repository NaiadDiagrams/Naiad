public class FlowchartRendererTests
{
    [Test]
    public Task Render_SimpleFlowchart()
    {
        const string input = """
            flowchart LR
                A[Start] --> B[Process] --> C[End]
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg);
    }

    [Test]
    public Task Render_FlowchartWithShapes()
    {
        const string input = """
            flowchart TD
                A[Rectangle]
                B(Rounded)
                C{Diamond}
                D((Circle))
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg);
    }

    [Test]
    public Task Render_FlowchartWithEdgeLabels()
    {
        const string input = """
            flowchart LR
                A --> |Yes| B
                A --> |No| C
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg);
    }

    [Test]
    public Task Render_GraphKeyword()
    {
        const string input = """
            graph TD
                A --> B --> C
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg);
    }
}
