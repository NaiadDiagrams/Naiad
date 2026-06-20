namespace Benchmarks;

[MemoryDiagnoser]
public class ParseBenchmarks
{
    const string pie = """
        pie
            "Dogs" : 40
            "Cats" : 30
            "Birds" : 20
            "Fish" : 10
        """;

    const string flowchart = """
        flowchart LR
            A[Start] --> B[Process] --> C[End]
        """;

    const string flowchartComplex = """
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

    const string sequence = """
        sequenceDiagram
            Alice->>Bob: Hello Bob
            Bob-->>Alice: Hi Alice
        """;

    const string @class = """
        classDiagram
            class Animal
        """;

    const string state = """
        stateDiagram-v2
            [*] --> Still
            Still --> [*]
        """;

    const string er = """
        erDiagram
            CUSTOMER ||--o{ ORDER : places
        """;

    const string gitGraph = """
        gitGraph
            commit
            commit
            commit
        """;

    const string gantt = """
        gantt
            title Simple Gantt
            Task A :a1, 2024-01-01, 30d
            Task B :b1, 2024-01-15, 20d
        """;

    const string mindmap = """
        mindmap
          Root
            Branch A
            Branch B
            Branch C
        """;

    const string timeline = """
        timeline
            2020 : Event One
            2021 : Event Two
            2022 : Event Three
        """;

    const string userJourney = """
        journey
            title My Working Day
            section Morning
                Make coffee: 5: Me
                Check emails: 3: Me
        """;

    const string quadrant = """
        quadrantChart
            title Campaign Analysis
            x-axis Low Reach --> High Reach
            y-axis Low Engagement --> High Engagement
            Campaign A: [0.3, 0.6]
            Campaign B: [0.7, 0.8]
        """;

    const string xyChart = """
        xychart-beta
            title "Monthly Sales"
            x-axis [Jan, Feb, Mar, Apr, May]
            y-axis "Revenue" 0 --> 100
            bar [50, 60, 75, 80, 90]
        """;

    const string sankey = """
        sankey-beta
        A,B,10
        A,C,20
        """;

    const string block = """
        block-beta
            columns 3
            a["Block A"] b["Block B"] c["Block C"]
        """;

    const string kanban = """
        kanban
        todo[Todo]
            task1[First Task]
            task2[Second Task]
        done[Done]
            task3[Completed Task]
        """;

    const string packet = """
        packet-beta
        0-15: "Source Port"
        16-31: "Destination Port"
        """;

    const string c4Context = """
        C4Context
            title System Context diagram
            Person(user, "User", "A user of the system")
            System(system, "System", "The main system")
            Rel(user, system, "Uses")
        """;

    const string requirement = """
        requirementDiagram

        requirement test_req {
            id: 1
            text: The system shall do something
            risk: high
            verifymethod: test
        }
        """;

    const string architecture = """
        architecture-beta
        service db(database)[Database]
        """;

    const string radar = """
        radar-beta
        axis A, B, C, D, E
        curve data1["Series1"]{20, 40, 60, 80, 50}
        """;

    [Benchmark] public PieModel Pie_Parse() => new PieParser().Parse(pie).Value;
    [Benchmark] public FlowchartModel Flowchart_Simple_Parse() => new FlowchartParser().Parse(flowchart).Value;
    [Benchmark] public FlowchartModel Flowchart_Complex_Parse() => new FlowchartParser().Parse(flowchartComplex).Value;
    [Benchmark] public SequenceModel Sequence_Parse() => new SequenceParser().Parse(sequence).Value;
    [Benchmark] public ClassModel Class_Parse() => new ClassParser().Parse(@class).Value;
    [Benchmark] public StateModel State_Parse() => new StateParser().Parse(state).Value;
    [Benchmark] public ERModel ER_Parse() => new ERParser().Parse(er).Value;
    [Benchmark] public GitGraphModel GitGraph_Parse() => new GitGraphParser().Parse(gitGraph).Value;
    [Benchmark] public GanttModel Gantt_Parse() => new GanttParser().Parse(gantt).Value;
    [Benchmark] public MindmapModel Mindmap_Parse() => new MindmapParser().Parse(mindmap).Value;
    [Benchmark] public TimelineModel Timeline_Parse() => new TimelineParser().Parse(timeline).Value;
    [Benchmark] public UserJourneyModel UserJourney_Parse() => new UserJourneyParser().Parse(userJourney).Value;
    [Benchmark] public QuadrantModel Quadrant_Parse() => new QuadrantParser().Parse(quadrant).Value;
    [Benchmark] public XYChartModel XYChart_Parse() => new XYChartParser().Parse(xyChart).Value;
    [Benchmark] public SankeyModel Sankey_Parse() => new SankeyParser().Parse(sankey).Value;
    [Benchmark] public BlockModel Block_Parse() => new BlockParser().Parse(block).Value;
    [Benchmark] public KanbanModel Kanban_Parse() => new KanbanParser().Parse(kanban).Value;
    [Benchmark] public PacketModel Packet_Parse() => new PacketParser().Parse(packet).Value;
    [Benchmark] public C4Model C4Context_Parse() => new C4Parser().Parse(c4Context).Value;
    [Benchmark] public RequirementModel Requirement_Parse() => new RequirementParser().Parse(requirement).Value;
    [Benchmark] public ArchitectureModel Architecture_Parse() => new ArchitectureParser().Parse(architecture).Value;
    [Benchmark] public RadarModel Radar_Parse() => new RadarParser().Parse(radar).Value;

    [Benchmark] public FlowchartModel Flowchart_Large_Parse() => new FlowchartParser().Parse(LargeFixtures.Flowchart).Value;
    [Benchmark] public SequenceModel Sequence_Large_Parse() => new SequenceParser().Parse(LargeFixtures.Sequence).Value;
    [Benchmark] public ClassModel Class_Large_Parse() => new ClassParser().Parse(LargeFixtures.Class).Value;
    [Benchmark] public StateModel State_Large_Parse() => new StateParser().Parse(LargeFixtures.State).Value;
    [Benchmark] public ERModel ER_Large_Parse() => new ERParser().Parse(LargeFixtures.ER).Value;
    [Benchmark] public MindmapModel Mindmap_Large_Parse() => new MindmapParser().Parse(LargeFixtures.Mindmap).Value;
    [Benchmark] public GanttModel Gantt_Large_Parse() => new GanttParser().Parse(LargeFixtures.Gantt).Value;
}
