public class MindmapRendererTests : TestBase
{
    [Test]
    public Task SimpleHierarchy()
    {
        const string input =
            """
            mindmap
              Root
                Branch A
                Branch B
                Branch C
            """;

        return VerifySvg(input);
    }

    [Test]
    public Task NestedHierarchy()
    {
        const string input =
            """
            mindmap
              Root
                Branch 1
                  Sub 1.1
                  Sub 1.2
                Branch 2
                  Sub 2.1
            """;

        return VerifySvg(input);
    }

    [Test]
    public Task CircleShape()
    {
        const string input =
            """
            mindmap
              ((Central))
                Child 1
                Child 2
            """;

        return VerifySvg(input);
    }

    [Test]
    public Task SquareShape()
    {
        const string input =
            """
            mindmap
              [Square Root]
                [Square Child]
                Normal Child
            """;

        return VerifySvg(input);
    }

    [Test]
    public Task RoundedShape()
    {
        const string input =
            """
            mindmap
              (Rounded Root)
                (Rounded Child)
                Normal Child
            """;

        return VerifySvg(input);
    }

    [Test]
    public Task HexagonShape()
    {
        const string input =
            """
            mindmap
              {{Hexagon}}
                Child A
                Child B
            """;

        return VerifySvg(input);
    }

    [Test]
    public Task MixedShapes()
    {
        const string input =
            """
            mindmap
              ((Center))
                [Square]
                  Normal
                (Rounded)
                  {{Hex}}
            """;

        return VerifySvg(input);
    }

    [Test]
    public Task DeepHierarchy()
    {
        const string input =
            """
            mindmap
              Root
                Level 1
                  Level 2
                    Level 3
                      Level 4
                        Level 5
            """;

        return VerifySvg(input);
    }

    [Test]
    public Task WideTree()
    {
        const string input =
            """
            mindmap
              Center
                A
                B
                C
                D
                E
                F
            """;

        return VerifySvg(input);
    }

    [Test]
    public Task CompleteMindmap()
    {
        const string input =
            """
            mindmap
              ((Project))
                [Planning]
                  Requirements
                  Design
                [Development]
                  Frontend
                  Backend
                  Database
                [Testing]
                  Unit Tests
                  Integration
                [Deployment]
                  Staging
                  Production
            """;

        return VerifySvg(input);
    }
}
