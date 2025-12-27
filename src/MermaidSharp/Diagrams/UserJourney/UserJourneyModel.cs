using MermaidSharp.Models;

namespace MermaidSharp.Diagrams.UserJourney;

public class UserJourneyModel : DiagramBase
{
    public List<JourneySection> Sections { get; } = [];
}

public class JourneySection
{
    public string? Name { get; set; }
    public List<JourneyTask> Tasks { get; } = [];
}

public class JourneyTask
{
    public required string Name { get; init; }
    public int Score { get; init; } // 1-5 satisfaction score
    public List<string> Actors { get; init; } = [];
}
