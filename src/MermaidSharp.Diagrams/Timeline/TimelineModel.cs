using MermaidSharp.Core.Models;

namespace MermaidSharp.Diagrams.Timeline;

public class TimelineModel : DiagramBase
{
    public List<TimelineSection> Sections { get; } = [];
}

public class TimelineSection
{
    public string? Name { get; set; }
    public List<TimePeriod> Periods { get; } = [];
}

public class TimePeriod
{
    public required string Label { get; init; }
    public List<string> Events { get; } = [];

    // Layout properties
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
}
