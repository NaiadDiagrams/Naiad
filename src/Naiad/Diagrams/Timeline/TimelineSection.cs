namespace MermaidSharp.Diagrams.Timeline;

public class TimelineSection
{
    public string? Name { get; set; }
    public List<TimePeriod> Periods { get; } = [];
}