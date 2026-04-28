namespace Tests;

public class AllowHtmlElementsFalseTests
{
    private static void AssertNoForeignObject(string input)
    {
        var svg = Mermaid.Render(input, new()
        { AllowHtmlElements = false });

        Assert.That(svg, Does.Contain("<svg"));
        Assert.That(svg, Does.Not.Contain("<foreignObject"));
        Assert.That(svg, Does.Not.Contain("http://www.w3.org/1999/xhtml"));
        Assert.That(svg, Does.Not.Contain("<div"));
    }

    [Test]
    public void Pie()
    {
        const string input =
            """
            pie
                "Dogs" : 40
                "Cats" : 60
            """;

        AssertNoForeignObject(input);
    }

    [Test]
    public void Flowchart()
    {
        const string input =
            """
            flowchart LR
                A[fa:fa-car Car] -->|Yes| B[End]
            """;

        AssertNoForeignObject(input);
    }

    [Test]
    public void Sequence()
    {
        const string input =
            """
            sequenceDiagram
                Alice->>Bob: Hello
                Bob-->>Alice: Hi
            """;

        AssertNoForeignObject(input);
    }

    [Test]
    public void ClassDiagram()
    {
        const string input =
            """
            classDiagram
                class Animal
            """;

        AssertNoForeignObject(input);
    }

    [Test]
    public void StateDiagramV2()
    {
        const string input =
            """
            stateDiagram-v2
                [*] --> Still
                Still --> [*]
            """;

        AssertNoForeignObject(input);
    }

    [Test]
    public void EntityRelationship()
    {
        const string input =
            """
            erDiagram
                CUSTOMER ||--o{ ORDER : places
                ORDER ||--|{ LINE-ITEM : contains
            """;

        AssertNoForeignObject(input);
    }

    [Test]
    public void GitGraph()
    {
        const string input =
            """
            gitGraph
                commit
                commit
            """;

        AssertNoForeignObject(input);
    }

    [Test]
    public void Gantt()
    {
        const string input =
            """
            gantt
                title Simple
                Task A :a1, 2024-01-01, 10d
            """;

        AssertNoForeignObject(input);
    }

    [Test]
    public void Mindmap()
    {
        const string input =
            """
            mindmap
              Root
                Branch
            """;

        AssertNoForeignObject(input);
    }

    [Test]
    public void Timeline()
    {
        const string input =
            """
            timeline
                2020 : Event One
            """;

        AssertNoForeignObject(input);
    }

    [Test]
    public void UserJourney()
    {
        const string input =
            """
            journey
                title My Working Day
                section Morning
                    Make coffee: 5: Me
            """;

        AssertNoForeignObject(input);
    }

    [Test]
    public void Quadrant()
    {
        const string input =
            """
            quadrantChart
                title Reach and impact
                x-axis Low --> High
                y-axis Low --> High
                quadrant-1 We should expand
                A: [0.3, 0.6]
            """;

        AssertNoForeignObject(input);
    }

    [Test]
    public void XYChart()
    {
        const string input =
            """
            xychart-beta
                title "Monthly Sales"
                x-axis [Jan, Feb]
                y-axis "Revenue" 0 --> 100
                bar [50, 60]
            """;

        AssertNoForeignObject(input);
    }

    [Test]
    public void Sankey()
    {
        const string input =
            """
            sankey-beta
            A,B,10
            """;

        AssertNoForeignObject(input);
    }

    [Test]
    public void Block()
    {
        const string input =
            """
            block-beta
                columns 2
                a["A"] b["B"]
            """;

        AssertNoForeignObject(input);
    }

    [Test]
    public void Kanban()
    {
        const string input =
            """
            kanban
            todo[Todo]
                task1[First Task]
            """;

        AssertNoForeignObject(input);
    }

    [Test]
    public void Packet()
    {
        const string input =
            """
            packet-beta
            0-15: \"Source Port\"
            """;

        AssertNoForeignObject(input);
    }

    [Test]
    public void C4Context()
    {
        const string input =
            """
            C4Context
                title System Context diagram
                Person(user, "User", "A user of the system")
                System(system, "System", "The main system")
                Rel(user, system, "Uses")
            """;

        AssertNoForeignObject(input);
    }

    [Test]
    public void Requirement()
    {
        const string input =
            """
            requirementDiagram

            requirement req1 {
                id: 1
                text: The system shall do something
                risk: high
                verifymethod: test
            }
            """;

        AssertNoForeignObject(input);
    }

    [Test]
    public void Architecture()
    {
        const string input =
            """
            architecture-beta
            service db(database)[Database]
            """;

        AssertNoForeignObject(input);
    }

    [Test]
    public void Radar()
    {
        const string input =
            """
            radar-beta
            title Skills
            axis A, B
            curve c1["Series 1"]{10, 20}
            """;

        AssertNoForeignObject(input);
    }

    [Test]
    public void Treemap()
    {
        const string input =
            """
            treemap-beta
            "A": 10
            "B": 20
            """;

        AssertNoForeignObject(input);
    }
}
