# <img src="/src/icon.png" height="30px"> Naiad

[![Build status](https://img.shields.io/github/actions/workflow/status/NaiadDiagrams/Naiad/ci.yml?branch=main)](https://github.com/NaiadDiagrams/Naiad/actions/workflows/ci.yml)
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

 * [Flowchart / Graph](https://mermaid.js.org/syntax/flowchart.html)
 * [Sequence Diagram](https://mermaid.js.org/syntax/sequenceDiagram.html)
 * [Class Diagram](https://mermaid.js.org/syntax/classDiagram.html)
 * [State Diagram](https://mermaid.js.org/syntax/stateDiagram.html)
 * [Entity Relationship Diagram](https://mermaid.js.org/syntax/entityRelationshipDiagram.html)
 * [Gantt Chart](https://mermaid.js.org/syntax/gantt.html)
 * [Pie Chart](https://mermaid.js.org/syntax/pie.html)
 * [Git Graph](https://mermaid.js.org/syntax/gitgraph.html)
 * [Mindmap](https://mermaid.js.org/syntax/mindmap.html)
 * [Timeline](https://mermaid.js.org/syntax/timeline.html)
 * [User Journey](https://mermaid.js.org/syntax/userJourney.html)
 * [Quadrant Chart](https://mermaid.js.org/syntax/quadrantChart.html)
 * [Requirement Diagram](https://mermaid.js.org/syntax/requirementDiagram.html)
 * [C4 Diagrams](https://mermaid.js.org/syntax/c4.html) (Context, Container, Component, Deployment)
 * [Kanban](https://mermaid.js.org/syntax/kanban.html)
 * [XY Chart](https://mermaid.js.org/syntax/xyChart.html) (beta)
 * [Sankey](https://mermaid.js.org/syntax/sankey.html) (beta)
 * [Block Diagram](https://mermaid.js.org/syntax/block.html) (beta)
 * [Packet Diagram](https://mermaid.js.org/syntax/packet.html) (beta)
 * [Architecture](https://mermaid.js.org/syntax/architecture.html) (beta)
 * [Radar](https://mermaid.js.org/syntax/radar.html) (beta)
 * [Treemap](https://mermaid.js.org/syntax/treemap.html) (beta)


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

[Naiad](https://thenounproject.com/icon/naiad-1389186/) designed by [Icons Producer](https://thenounproject.com/creator/iconsproducer/) from [The Noun Project](https://thenounproject.com).
