using Naiad.Diagrams.C4;

public class C4ParserTests
{
    [Test]
    public async Task CapturesRelationshipDirections()
    {
        const string input =
            """
            C4Context
                System(a, "A")
                System(b, "B")
                Rel(a, b, "default")
                Rel_D(a, b, "down")
                Rel_U(a, b, "up")
                Rel_L(a, b, "left")
                Rel_R(a, b, "right")
                Rel_Back(b, a, "back")
                Rel_Neighbor(a, b, "neighbor")
            """;

        var result = new C4Parser().Parse(input);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Value.Relationships.Select(_ => _.Direction)).IsEquivalentTo(
            new[]
            {
                C4RelationshipDirection.Default,
                C4RelationshipDirection.Down,
                C4RelationshipDirection.Up,
                C4RelationshipDirection.Left,
                C4RelationshipDirection.Right,
                C4RelationshipDirection.Back,
                C4RelationshipDirection.Neighbor
            });
    }

    [Test]
    public async Task CapturesRelationshipTechnology()
    {
        const string input =
            """
            C4Context
                System(a, "A")
                System(b, "B")
                Rel(a, b, "Uses", "HTTPS")
            """;

        var result = new C4Parser().Parse(input);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Value.Relationships.Count).IsEqualTo(1);
        await Assert.That(result.Value.Relationships[0].Technology).IsEqualTo("HTTPS");
    }
}
