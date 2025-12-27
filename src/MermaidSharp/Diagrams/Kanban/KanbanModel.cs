using MermaidSharp.Models;

namespace MermaidSharp.Diagrams.Kanban;

public class KanbanModel : DiagramBase
{
    public List<KanbanColumn> Columns { get; } = [];
}

public class KanbanColumn
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public List<KanbanTask> Tasks { get; } = [];
}

public class KanbanTask
{
    public required string Id { get; init; }
    public required string Name { get; init; }
}
