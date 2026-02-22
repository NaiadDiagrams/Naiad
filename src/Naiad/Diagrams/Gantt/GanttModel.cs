namespace MermaidSharp.Diagrams.Gantt;

public class GanttModel : DiagramBase
{
    public string DateFormat { get; set; } = "YYYY-MM-DD";
    public string? AxisFormat { get; set; }
    public bool ExcludeWeekends { get; set; }
    public List<string> ExcludeDays { get; } = [];
    public List<GanttSection> Sections { get; } = [];
}