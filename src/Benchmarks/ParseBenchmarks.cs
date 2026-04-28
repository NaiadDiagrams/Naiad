using BenchmarkDotNet.Attributes;
using MermaidSharp.Diagrams.Architecture;
using MermaidSharp.Diagrams.Block;
using MermaidSharp.Diagrams.C4;
using MermaidSharp.Diagrams.Class;
using MermaidSharp.Diagrams.EntityRelationship;
using MermaidSharp.Diagrams.Flowchart;
using MermaidSharp.Diagrams.Gantt;
using MermaidSharp.Diagrams.GitGraph;
using MermaidSharp.Diagrams.Kanban;
using MermaidSharp.Diagrams.Mindmap;
using MermaidSharp.Diagrams.Packet;
using MermaidSharp.Diagrams.Pie;
using MermaidSharp.Diagrams.Quadrant;
using MermaidSharp.Diagrams.Radar;
using MermaidSharp.Diagrams.Requirement;
using MermaidSharp.Diagrams.Sankey;
using MermaidSharp.Diagrams.Sequence;
using MermaidSharp.Diagrams.State;
using MermaidSharp.Diagrams.Timeline;
using MermaidSharp.Diagrams.UserJourney;
using MermaidSharp.Diagrams.XYChart;

namespace Benchmarks;

[MemoryDiagnoser]
public class ParseBenchmarks
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

    [Benchmark] public PieModel Pie_Parse() => new PieParser().Parse(Pie).Value;
    [Benchmark] public FlowchartModel Flowchart_Simple_Parse() => new FlowchartParser().Parse(Flowchart).Value;
    [Benchmark] public FlowchartModel Flowchart_Complex_Parse() => new FlowchartParser().Parse(FlowchartComplex).Value;
    [Benchmark] public SequenceModel Sequence_Parse() => new SequenceParser().Parse(Sequence).Value;
    [Benchmark] public ClassModel Class_Parse() => new ClassParser().Parse(Class).Value;
    [Benchmark] public StateModel State_Parse() => new StateParser().Parse(State).Value;
    [Benchmark] public ERModel ER_Parse() => new ERParser().Parse(ER).Value;
    [Benchmark] public GitGraphModel GitGraph_Parse() => new GitGraphParser().Parse(GitGraph).Value;
    [Benchmark] public GanttModel Gantt_Parse() => new GanttParser().Parse(Gantt).Value;
    [Benchmark] public MindmapModel Mindmap_Parse() => new MindmapParser().Parse(Mindmap).Value;
    [Benchmark] public TimelineModel Timeline_Parse() => new TimelineParser().Parse(Timeline).Value;
    [Benchmark] public UserJourneyModel UserJourney_Parse() => new UserJourneyParser().Parse(UserJourney).Value;
    [Benchmark] public QuadrantModel Quadrant_Parse() => new QuadrantParser().Parse(Quadrant).Value;
    [Benchmark] public XYChartModel XYChart_Parse() => new XYChartParser().Parse(XYChart).Value;
    [Benchmark] public SankeyModel Sankey_Parse() => new SankeyParser().Parse(Sankey).Value;
    [Benchmark] public BlockModel Block_Parse() => new BlockParser().Parse(Block).Value;
    [Benchmark] public KanbanModel Kanban_Parse() => new KanbanParser().Parse(Kanban).Value;
    [Benchmark] public PacketModel Packet_Parse() => new PacketParser().Parse(Packet).Value;
    [Benchmark] public C4Model C4Context_Parse() => new C4Parser().Parse(C4Context).Value;
    [Benchmark] public RequirementModel Requirement_Parse() => new RequirementParser().Parse(Requirement).Value;
    [Benchmark] public ArchitectureModel Architecture_Parse() => new ArchitectureParser().Parse(Architecture).Value;
    [Benchmark] public RadarModel Radar_Parse() => new RadarParser().Parse(Radar).Value;
}
