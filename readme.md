# <img src="/src/icon.png" height="30px"> Naiad

[![Build status](https://img.shields.io/appveyor/build/SimonCropp/Naiad)](https://ci.appveyor.com/project/SimonCropp/Naiad)
[![NuGet Status](https://img.shields.io/nuget/v/Naiad.svg)](https://www.nuget.org/packages/Naiad/)

A .NET library for rendering [Mermaid](https://mermaid.js.org/) diagrams to SVG. No browser or JavaScript runtime required.


## NuGet package

https://nuget.org/packages/Naiad/


## Usage

```cs
var svg = Mermaid.Render(
    """
    flowchart LR
        A[Start] --> B[Process] --> C[End]
    """);
```

The diagram type is automatically detected from the input.


### Render Options

```cs
var svg = Mermaid.Render(
    input,
    new RenderOptions
    {
        Padding = 20,
        FontSize = 14,
        FontFamily = "Arial, sans-serif"
    });
```


## Supported Diagram Types

 * Flowchart / Graph
 * Sequence Diagram
 * Class Diagram
 * State Diagram
 * Entity Relationship Diagram
 * Gantt Chart
 * Pie Chart
 * Git Graph
 * Mindmap
 * Timeline
 * User Journey
 * Quadrant Chart
 * Requirement Diagram
 * C4 Diagrams (Context, Container, Component, Deployment)
 * Kanban
 * XY Chart (beta)
 * Sankey (beta)
 * Block Diagram (beta)
 * Packet Diagram (beta)
 * Architecture (beta)
 * Radar (beta)
 * Treemap (beta)


## Test Renders<!-- include: renders. path: /src/test-renders/renders.include.md -->

Auto-generated documentation from the test suite.

- [C4](/src/test-renders/C4.md)
- [Class](/src/test-renders/Class.md)
- [EntityRelationship](/src/test-renders/EntityRelationship.md)
- [Flowchart](/src/test-renders/Flowchart.md)
- [Gantt](/src/test-renders/Gantt.md)
- [GitGraph](/src/test-renders/GitGraph.md)
- [Kanban](/src/test-renders/Kanban.md)
- [Mindmap](/src/test-renders/Mindmap.md)
- [Pie](/src/test-renders/Pie.md)
- [Quadrant](/src/test-renders/Quadrant.md)
- [Requirement](/src/test-renders/Requirement.md)
- [Sequence](/src/test-renders/Sequence.md)
- [State](/src/test-renders/State.md)
- [Timeline](/src/test-renders/Timeline.md)
- [UserJourney](/src/test-renders/UserJourney.md)

### Beta diagram types

- [Architecture](Architecture.md)
- [Block](Block.md)
- [Packet](Packet.md)
- [Radar](Radar.md)
- [Sankey](Sankey.md)
- [Treemap](Treemap.md)
- [XYChart](XYChart.md)<!-- endInclude -->


## Icon

[Mermaid Tail](https://thenounproject.com/icon/mermaid-tail-1908145//) designed by [Olena Panasovska](https://thenounproject.com/creator/zzyzz/) from [The Noun Project](https://thenounproject.com).
