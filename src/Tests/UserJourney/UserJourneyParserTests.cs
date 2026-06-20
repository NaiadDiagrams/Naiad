using Naiad.Diagrams.UserJourney;

public class UserJourneyParserTests
{
    [Test]
    public async Task DoesNotClampScore()
    {
        // Mermaid imposes no 1-5 range on journey scores, so Naiad preserves the raw value
        // rather than silently clamping it.
        const string input =
            """
            journey
                section Experience
                    Overjoyed: 7: Me
            """;

        var result = new UserJourneyParser().Parse(input);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Value.Sections[0].Tasks[0].Score).IsEqualTo(7);
    }
}
