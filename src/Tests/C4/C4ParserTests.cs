using Naiad.Diagrams.C4;

public class C4ParserTests
{
    [Test]
    public void CapturesRelationshipDirections()
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

        Assert.That(result.Success, Is.True);
        Assert.That(
            result.Value.Relationships.Select(_ => _.Direction),
            Is.EqualTo(new[]
            {
                C4RelationshipDirection.Default,
                C4RelationshipDirection.Down,
                C4RelationshipDirection.Up,
                C4RelationshipDirection.Left,
                C4RelationshipDirection.Right,
                C4RelationshipDirection.Back,
                C4RelationshipDirection.Neighbor
            }));
    }

    [Test]
    public void CapturesRelationshipTechnology()
    {
        const string input =
            """
            C4Context
                System(a, "A")
                System(b, "B")
                Rel(a, b, "Uses", "HTTPS")
            """;

        var result = new C4Parser().Parse(input);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Value.Relationships, Has.Count.EqualTo(1));
        Assert.That(result.Value.Relationships[0].Technology, Is.EqualTo("HTTPS"));
    }
}
