namespace MermaidSharp.Diagrams.Sequence;

public class Participant
{
    public required string Id { get; init; }
    public string? Alias { get; set; }
    public ParticipantType Type { get; set; } = ParticipantType.Participant;

    public string DisplayName => Alias ?? Id;
}