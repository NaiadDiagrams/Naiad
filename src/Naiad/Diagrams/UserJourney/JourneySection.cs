namespace MermaidSharp.Diagrams.UserJourney;

public class JourneySection
{
    public string? Name { get; set; }
    public List<JourneyTask> Tasks { get; } = [];
}