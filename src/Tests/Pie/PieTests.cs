public class PieTests : TestBase
{
    [Test]
    public Task Simple()
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
    public Task Title()
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
    public Task ShowData()
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
