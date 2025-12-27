using MermaidSharp.Diagrams.Pie;

namespace MermaidSharp.Tests.Pie;

public class PieParserTests
{
    [Test]
    public async Task Parse_SimplePie_ReturnsSections()
    {
        const string input = """
            pie
                "Dogs" : 40
                "Cats" : 30
                "Birds" : 20
            """;

        var parser = new PieParser();
        var result = parser.Parse(input);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Value.Sections.Count).IsEqualTo(3);
        await Assert.That(result.Value.Sections[0].Label).IsEqualTo("Dogs");
        await Assert.That(result.Value.Sections[0].Value).IsEqualTo(40);
    }

    [Test]
    public async Task Parse_PieWithTitle_ParsesTitle()
    {
        const string input = """
            pie
                title Pet Distribution
                "Dogs" : 40
                "Cats" : 30
            """;

        var parser = new PieParser();
        var result = parser.Parse(input);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Value.Title).IsEqualTo("Pet Distribution");
        await Assert.That(result.Value.Sections.Count).IsEqualTo(2);
    }

    [Test]
    public async Task Parse_PieWithShowData_SetsShowDataFlag()
    {
        const string input = """
            pie showData
                "A" : 50
                "B" : 50
            """;

        var parser = new PieParser();
        var result = parser.Parse(input);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Value.ShowData).IsTrue();
    }

    [Test]
    public async Task Parse_PieWithDecimalValues_ParsesCorrectly()
    {
        const string input = """
            pie
                "Section A" : 33.33
                "Section B" : 66.67
            """;

        var parser = new PieParser();
        var result = parser.Parse(input);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Value.Sections[0].Value).IsEqualTo(33.33);
        await Assert.That(result.Value.Sections[1].Value).IsEqualTo(66.67);
    }
}
