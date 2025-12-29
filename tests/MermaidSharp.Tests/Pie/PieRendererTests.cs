public class PieRendererTests : TestBase
{
    [Test]
    public Task SimplePie()
    {
        const string input =
            """
            pie
                "Dogs" : 40
                "Cats" : 30
                "Birds" : 20
                "Fish" : 10
            """;

        return VerifySvg(input);
    }

    [Test]
    public Task PieWithTitle()
    {
        const string input =
            """
            pie
                title Pet Distribution
                "Dogs" : 40
                "Cats" : 30
                "Birds" : 30
            """;

        return VerifySvg(input);
    }

    [Test]
    public Task PieWithShowData()
    {
        const string input =
            """
            pie showData
                "Revenue" : 65
                "Costs" : 35
            """;

        return VerifySvg(input);
    }
}
