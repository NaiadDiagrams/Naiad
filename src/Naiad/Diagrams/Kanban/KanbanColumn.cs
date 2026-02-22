namespace MermaidSharp.Diagrams.Kanban;

public class KanbanColumn
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public List<KanbanTask> Tasks { get; } = [];
}