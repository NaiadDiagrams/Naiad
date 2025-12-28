using MermaidSharp.Diagrams.Pie;

public class PieParserTests
{
    [Test]
    public void SimplePie_ReturnsSections()
    {
        const string input =
            """
            pie
                "Dogs" : 40
                "Cats" : 30
                "Birds" : 20
            """;

        var parser = new PieParser();
        var result = parser.Parse(input);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Value.Sections.Count, Is.EqualTo(3));
        Assert.That(result.Value.Sections[0].Label, Is.EqualTo("Dogs"));
        Assert.That(result.Value.Sections[0].Value, Is.EqualTo(40));
    }

    [Test]
    public void PieWithTitle_ParsesTitle()
    {
        const string input =
            """
            pie
                title Pet Distribution
                "Dogs" : 40
                "Cats" : 30
            """;

        var parser = new PieParser();
        var result = parser.Parse(input);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Value.Title, Is.EqualTo("Pet Distribution"));
        Assert.That(result.Value.Sections.Count, Is.EqualTo(2));
    }

    [Test]
    public void PieWithShowData_SetsShowDataFlag()
    {
        const string input =
            """
            pie showData
                "A" : 50
                "B" : 50
            """;

        var parser = new PieParser();
        var result = parser.Parse(input);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Value.ShowData, Is.True);
    }

    [Test]
    public void PieWithDecimalValues_ParsesCorrectly()
    {
        const string input =
            """
            pie
                "Section A" : 33.33
                "Section B" : 66.67
            """;

        var parser = new PieParser();
        var result = parser.Parse(input);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Value.Sections[0].Value, Is.EqualTo(33.33));
        Assert.That(result.Value.Sections[1].Value, Is.EqualTo(66.67));
    }
}
