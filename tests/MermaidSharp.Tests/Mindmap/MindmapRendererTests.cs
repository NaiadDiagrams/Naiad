using TUnit.Core;

namespace MermaidSharp.Tests.Mindmap;

public class MindmapRendererTests
{
    [Test]
    public Task Render_SimpleHierarchy()
    {
        const string input = """
            mindmap
              Root
                Branch A
                Branch B
                Branch C
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg);
    }

    [Test]
    public Task Render_NestedHierarchy()
    {
        const string input = """
            mindmap
              Root
                Branch 1
                  Sub 1.1
                  Sub 1.2
                Branch 2
                  Sub 2.1
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg);
    }

    [Test]
    public Task Render_CircleShape()
    {
        const string input = """
            mindmap
              ((Central))
                Child 1
                Child 2
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg);
    }

    [Test]
    public Task Render_SquareShape()
    {
        const string input = """
            mindmap
              [Square Root]
                [Square Child]
                Normal Child
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg);
    }

    [Test]
    public Task Render_RoundedShape()
    {
        const string input = """
            mindmap
              (Rounded Root)
                (Rounded Child)
                Normal Child
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg);
    }

    [Test]
    public Task Render_HexagonShape()
    {
        const string input = """
            mindmap
              {{Hexagon}}
                Child A
                Child B
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg);
    }

    [Test]
    public Task Render_MixedShapes()
    {
        const string input = """
            mindmap
              ((Center))
                [Square]
                  Normal
                (Rounded)
                  {{Hex}}
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg);
    }

    [Test]
    public Task Render_DeepHierarchy()
    {
        const string input = """
            mindmap
              Root
                Level 1
                  Level 2
                    Level 3
                      Level 4
                        Level 5
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg);
    }

    [Test]
    public Task Render_WideTree()
    {
        const string input = """
            mindmap
              Center
                A
                B
                C
                D
                E
                F
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg);
    }

    [Test]
    public Task Render_CompleteMindmap()
    {
        const string input = """
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

        var svg = Mermaid.Render(input);
        return Verify(svg);
    }
}
