using BenchmarkDotNet.Attributes;
using MermaidSharp;

namespace Benchmarks;

[MemoryDiagnoser]
public class RenderBenchmarks
{
    const string Pie = """
        pie
            "Dogs" : 40
            "Cats" : 30
            "Birds" : 20
            "Fish" : 10
        """;

    const string Flowchart = """
        flowchart LR
            A[Start] --> B[Process] --> C[End]
        """;

    const string FlowchartComplex = """
        flowchart TD
            A[Start] --> B{Decision}
            B -->|Yes| C[Process 1]
            B -->|No| D[Process 2]
            C --> E[Merge]
            D --> E
            E --> F{Another Decision}
            F -->|Path 1| G[Result 1]
            F -->|Path 2| H[Result 2]
            F -->|Path 3| I[Result 3]
            G --> J[End]
            H --> J
            I --> J
        """;

    const string Sequence = """
        sequenceDiagram
            Alice->>Bob: Hello Bob
            Bob-->>Alice: Hi Alice
        """;

    const string Class = """
        classDiagram
            class Animal
        """;

    const string State = """
        stateDiagram-v2
            [*] --> Still
            Still --> [*]
        """;

    const string ER = """
        erDiagram
            CUSTOMER ||--o{ ORDER : places
        """;

    const string GitGraph = """
        gitGraph
            commit
            commit
            commit
        """;

    const string Gantt = """
        gantt
            title Simple Gantt
            Task A :a1, 2024-01-01, 30d
            Task B :b1, 2024-01-15, 20d
        """;

    const string Mindmap = """
        mindmap
          Root
            Branch A
            Branch B
            Branch C
        """;

    const string Timeline = """
        timeline
            2020 : Event One
            2021 : Event Two
            2022 : Event Three
        """;

    const string UserJourney = """
        journey
            title My Working Day
            section Morning
                Make coffee: 5: Me
                Check emails: 3: Me
        """;

    const string Quadrant = """
        quadrantChart
            title Campaign Analysis
            x-axis Low Reach --> High Reach
            y-axis Low Engagement --> High Engagement
            Campaign A: [0.3, 0.6]
            Campaign B: [0.7, 0.8]
        """;

    const string XYChart = """
        xychart-beta
            title "Monthly Sales"
            x-axis [Jan, Feb, Mar, Apr, May]
            y-axis "Revenue" 0 --> 100
            bar [50, 60, 75, 80, 90]
        """;

    const string Sankey = """
        sankey-beta
        A,B,10
        A,C,20
        """;

    const string Block = """
        block-beta
            columns 3
            a["Block A"] b["Block B"] c["Block C"]
        """;

    const string Kanban = """
        kanban
        todo[Todo]
            task1[First Task]
            task2[Second Task]
        done[Done]
            task3[Completed Task]
        """;

    const string Packet = """
        packet-beta
        0-15: "Source Port"
        16-31: "Destination Port"
        """;

    const string C4Context = """
        C4Context
            title System Context diagram
            Person(user, "User", "A user of the system")
            System(system, "System", "The main system")
            Rel(user, system, "Uses")
        """;

    const string Requirement = """
        requirementDiagram

        requirement test_req {
            id: 1
            text: The system shall do something
            risk: high
            verifymethod: test
        }
        """;

    const string Architecture = """
        architecture-beta
        service db(database)[Database]
        """;

    const string Radar = """
        radar-beta
        axis A, B, C, D, E
        curve data1["Series1"]{20, 40, 60, 80, 50}
        """;

    const string Treemap = """
        treemap-beta
        "Section A"
            "Item 1": 30
            "Item 2": 20
        "Section B"
            "Item 3": 50
        """;

    [Benchmark] public string Pie_Render() => Mermaid.Render(Pie);
    [Benchmark] public string Flowchart_Simple() => Mermaid.Render(Flowchart);
    [Benchmark] public string Flowchart_Complex() => Mermaid.Render(FlowchartComplex);
    [Benchmark] public string Sequence_Render() => Mermaid.Render(Sequence);
    [Benchmark] public string Class_Render() => Mermaid.Render(Class);
    [Benchmark] public string State_Render() => Mermaid.Render(State);
    [Benchmark] public string ER_Render() => Mermaid.Render(ER);
    [Benchmark] public string GitGraph_Render() => Mermaid.Render(GitGraph);
    [Benchmark] public string Gantt_Render() => Mermaid.Render(Gantt);
    [Benchmark] public string Mindmap_Render() => Mermaid.Render(Mindmap);
    [Benchmark] public string Timeline_Render() => Mermaid.Render(Timeline);
    [Benchmark] public string UserJourney_Render() => Mermaid.Render(UserJourney);
    [Benchmark] public string Quadrant_Render() => Mermaid.Render(Quadrant);
    [Benchmark] public string XYChart_Render() => Mermaid.Render(XYChart);
    [Benchmark] public string Sankey_Render() => Mermaid.Render(Sankey);
    [Benchmark] public string Block_Render() => Mermaid.Render(Block);
    [Benchmark] public string Kanban_Render() => Mermaid.Render(Kanban);
    [Benchmark] public string Packet_Render() => Mermaid.Render(Packet);
    [Benchmark] public string C4Context_Render() => Mermaid.Render(C4Context);
    [Benchmark] public string Requirement_Render() => Mermaid.Render(Requirement);
    [Benchmark] public string Architecture_Render() => Mermaid.Render(Architecture);
    [Benchmark] public string Radar_Render() => Mermaid.Render(Radar);
    [Benchmark] public string Treemap_Render() => Mermaid.Render(Treemap);
}
