public class RadarTests
{
    [Test]
    public async Task BasicRadar()
    {
        var input = """
            radar-beta
            axis A, B, C, D, E
            curve data1["Series1"]{20, 40, 60, 80, 50}
            """;

        var svg = Mermaid.Render(input);
        await Verify(svg, extension: "svg");
    }

    [Test]
    public async Task RadarWithTitle()
    {
        var input = """
            radar-beta
            title Performance Metrics
            axis Speed, Quality, Cost, Time, Scope
            curve a["Project A"]{80, 60, 40, 70, 90}
            """;

        var svg = Mermaid.Render(input);
        await Verify(svg, extension: "svg");
    }

    [Test]
    public async Task MultipleCurves()
    {
        var input = """
            radar-beta
            title Comparison
            axis Strength, Speed, Agility, Stamina, Intelligence
            curve hero["Hero"]{90, 70, 85, 60, 75}
            curve villain["Villain"]{75, 80, 65, 85, 90}
            """;

        var svg = Mermaid.Render(input);
        await Verify(svg, extension: "svg");
    }

    [Test]
    public async Task ThreeCurves()
    {
        var input = """
            radar-beta
            title Language Skills
            axis English, French, German, Spanish
            curve a["User1"]{80, 30, 40, 50}
            curve b["User2"]{50, 80, 30, 40}
            curve c["User3"]{60, 60, 80, 70}
            """;

        var svg = Mermaid.Render(input);
        await Verify(svg, extension: "svg");
    }

    [Test]
    public async Task TriangleRadar()
    {
        var input = """
            radar-beta
            axis A, B, C
            curve data["Values"]{100, 80, 60}
            """;

        var svg = Mermaid.Render(input);
        await Verify(svg, extension: "svg");
    }

    [Test]
    public async Task HexagonRadar()
    {
        var input = """
            radar-beta
            title Six Dimensions
            axis Dim1, Dim2, Dim3, Dim4, Dim5, Dim6
            curve data["Metrics"]{50, 80, 60, 90, 40, 70}
            """;

        var svg = Mermaid.Render(input);
        await Verify(svg, extension: "svg");
    }
}
