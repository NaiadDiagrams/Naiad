using MermaidSharp.Models;
using Pidgin;

namespace MermaidSharp;

public interface IDiagramParser<TModel> where TModel : DiagramBase
{
    DiagramType DiagramType { get; }
    Result<char, TModel> Parse(string input);
}

public enum DiagramType
{
    Flowchart,
    Sequence,
    Class,
    State,
    EntityRelationship,
    Gantt,
    Pie,
    GitGraph,
    Mindmap,
    Timeline,
    C4Context,
    C4Container,
    C4Component,
    C4Deployment,
    Block,
    Kanban,
    Quadrant,
    Requirement,
    Sankey,
    UserJourney,
    XYChart,
    Architecture,
    Packet,
    Radar,
    Treemap,
    ZenUML
}
