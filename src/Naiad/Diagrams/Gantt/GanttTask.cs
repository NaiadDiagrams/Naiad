namespace MermaidSharp.Diagrams.Gantt;

public class GanttTask
{
    public required string Name { get; init; }
    public string? Id { get; set; }
    public DateTime? StartDate { get; set; }
    public string? AfterTaskId { get; set; }
    public TimeSpan? Duration { get; set; }
    public DateTime? EndDate { get; set; }
    public GanttTaskStatus Status { get; set; } = GanttTaskStatus.None;
    public bool IsCritical { get; set; }
    public bool IsMilestone { get; set; }

    // Computed properties for rendering
    public DateTime ComputedStart { get; set; }
    public DateTime ComputedEnd { get; set; }
    public int Row { get; set; }
    public string? SectionName { get; set; }
}