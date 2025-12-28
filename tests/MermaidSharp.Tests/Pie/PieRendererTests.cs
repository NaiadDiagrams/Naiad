public class PieRendererTests
{
    [Test]
    public Task Render_SimplePie()
    {
        const string input =
            """
            pie
                "Dogs" : 40
                "Cats" : 30
                "Birds" : 20
                "Fish" : 10
            """;

        return SvgVerify.Verify(input);
    }

    [Test]
    public Task Render_PieWithTitle()
    {
        const string input =
            """
            pie
                title Pet Distribution
                "Dogs" : 40
                "Cats" : 30
                "Birds" : 30
            """;

        return SvgVerify.Verify(input);
    }

    [Test]
    public Task Render_PieWithShowData()
    {
        const string input =
            """
            pie showData
                "Revenue" : 65
                "Costs" : 35
            """;

        return SvgVerify.Verify(input);
    }
}
