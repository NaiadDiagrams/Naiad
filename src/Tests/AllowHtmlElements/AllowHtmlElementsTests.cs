namespace Tests;

// Covers RenderOptions.AllowHtmlElements = false: labels become native <text> at build time
// (no <foreignObject>) and the Font Awesome @import (xhtml) is dropped.
public class AllowHtmlElementsTests : TestBase
{
    static RenderOptions NoHtml => new() { AllowHtmlElements = false };

    const string SingleNode =
        """
        flowchart LR
            A[Hello]
        """;

    // The verified SVGs of these two are embedded in the readme via mdsnippets,
    // so the documented "with / without HTML" output stays in sync with the renderer.
    [Test]
    public Task SingleNodeHtml() => VerifySvg(SingleNode);

    [Test]
    public Task SingleNodeNoHtml() => VerifySvg(SingleNode, NoHtml);

    // Flowchart is one of only two diagram types that emit <foreignObject> (node + edge labels),
    // so snapshot its full no-HTML output: the fa icon is dropped, "Car"/"Yes"/"End" survive as <text>.
    [Test]
    public Task Flowchart() =>
        VerifySvg(
            """
            flowchart LR
                A[fa:fa-car Car] -->|Yes| B[End]
            """,
            NoHtml);

    // Mindmap is the other foreignObject emitter (icon glyphs). The fa icon is dropped; text remains.
    [Test]
    public Task Mindmap() =>
        VerifySvg(
            """
            mindmap
              Root
                Transport ::icon(fa fa-car)
                Storage
            """,
            NoHtml);

    // The default (HTML allowed) path must be unchanged: foreignObject + the Font Awesome import remain.
    [Test]
    public async Task DefaultKeepsForeignObject()
    {
        var svg = Mermaid.Render(
            """
            flowchart LR
                A[fa:fa-car Car] --> B[End]
            """);

        await Assert.That(svg).Contains("<foreignObject");
        await Assert.That(svg).Contains("http://www.w3.org/1999/xhtml");
    }

    [Test]
    [MethodDataSource(nameof(Diagrams))]
    public async Task EmitsNoHtmlMarkup(string name, string input)
    {
        var svg = Mermaid.Render(input, NoHtml);

        await Assert.That(svg).Contains("<svg").Because(name);
        await Assert.That(svg).DoesNotContain("<foreignObject").Because(name);
        await Assert.That(svg).DoesNotContain("<div").Because(name);
        // The Font Awesome @import is the one xhtml-namespaced node every diagram emits by default.
        await Assert.That(svg).DoesNotContain("http://www.w3.org/1999/xhtml").Because(name);
    }

    public static IEnumerable<(string Name, string Input)> Diagrams()
    {
        (string, string) Case(string name, string input) => (name, input);

        yield return Case("Pie",
            """
            pie
                "Dogs" : 40
                "Cats" : 60
            """);
        yield return Case("Flowchart",
            """
            flowchart LR
                A[fa:fa-car Car] -->|Yes| B[End]
            """);
        yield return Case("Sequence",
            """
            sequenceDiagram
                Alice->>Bob: Hello
                Bob-->>Alice: Hi
            """);
        yield return Case("ClassDiagram",
            """
            classDiagram
                class Animal
            """);
        yield return Case("StateDiagramV2",
            """
            stateDiagram-v2
                [*] --> Still
                Still --> [*]
            """);
        yield return Case("EntityRelationship",
            """
            erDiagram
                CUSTOMER ||--o{ ORDER : places
                ORDER ||--|{ LINE-ITEM : contains
            """);
        yield return Case("GitGraph",
            """
            gitGraph
                commit
                commit
            """);
        yield return Case("Gantt",
            """
            gantt
                title Simple
                Task A :a1, 2024-01-01, 10d
            """);
        yield return Case("Mindmap",
            """
            mindmap
              Root
                Transport ::icon(fa fa-car)
            """);
        yield return Case("Timeline",
            """
            timeline
                2020 : Event One
            """);
        yield return Case("UserJourney",
            """
            journey
                title My Working Day
                section Morning
                    Make coffee: 5: Me
            """);
        yield return Case("Quadrant",
            """
            quadrantChart
                title Reach and impact
                x-axis Low --> High
                y-axis Low --> High
                quadrant-1 We should expand
                A: [0.3, 0.6]
            """);
        yield return Case("XYChart",
            """
            xychart-beta
                title "Monthly Sales"
                x-axis [Jan, Feb]
                y-axis "Revenue" 0 --> 100
                bar [50, 60]
            """);
        yield return Case("Sankey",
            """
            sankey-beta
            A,B,10
            """);
        yield return Case("Block",
            """
            block-beta
                columns 2
                a["A"] b["B"]
            """);
        yield return Case("Kanban",
            """
            kanban
            todo[Todo]
                task1[First Task]
            """);
        yield return Case("Packet",
            """
            packet-beta
            0-15: "Source Port"
            """);
        yield return Case("C4Context",
            """
            C4Context
                title System Context diagram
                Person(user, "User", "A user of the system")
                System(system, "System", "The main system")
                Rel(user, system, "Uses")
            """);
        yield return Case("Requirement",
            """
            requirementDiagram

            requirement req1 {
                id: 1
                text: The system shall do something
                risk: high
                verifymethod: test
            }
            """);
        yield return Case("Architecture",
            """
            architecture-beta
            service db(database)[Database]
            """);
        yield return Case("Radar",
            """
            radar-beta
            title Skills
            axis A, B
            curve c1["Series 1"]{10, 20}
            """);
        yield return Case("Treemap",
            """
            treemap-beta
            "A": 10
            "B": 20
            """);
    }
}
