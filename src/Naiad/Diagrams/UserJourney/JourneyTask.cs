namespace MermaidSharp.Diagrams.UserJourney;

public class JourneyTask
{
    public required string Name { get; init; }
    public int Score { get; init; } // 1-5 satisfaction score
    public List<string> Actors { get; init; } = [];
}