public class PieRendererTests
{
    [Test]
    public Task Render_SimplePie()
    {
        const string input = """
            pie
                "Dogs" : 40
                "Cats" : 30
                "Birds" : 20
                "Fish" : 10
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg, extension: "svg");
    }

    [Test]
    public Task Render_PieWithTitle()
    {
        const string input = """
            pie
                title Pet Distribution
                "Dogs" : 40
                "Cats" : 30
                "Birds" : 30
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg, extension: "svg");
    }

    [Test]
    public Task Render_PieWithShowData()
    {
        const string input = """
            pie showData
                "Revenue" : 65
                "Costs" : 35
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg, extension: "svg");
    }
}
