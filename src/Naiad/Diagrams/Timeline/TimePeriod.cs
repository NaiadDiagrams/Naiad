namespace Naiad.Diagrams.Timeline;

public class TimePeriod
{
    public required string Label { get; init; }
    public List<string> Events { get; } = [];
}
