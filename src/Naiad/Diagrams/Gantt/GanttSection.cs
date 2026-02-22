namespace MermaidSharp.Diagrams.Gantt;

public class GanttSection
{
    public string Name { get; set; } = "";
    public List<GanttTask> Tasks { get; } = [];
}