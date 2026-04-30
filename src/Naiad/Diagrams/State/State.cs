namespace Naiad.Diagrams.State;

public class State
{
    public required string Id { get; init; }
    public string? Description { get; set; }
    public StateType Type { get; set; } = StateType.Normal;
    public List<State> NestedStates { get; } = [];
    public List<StateTransition> NestedTransitions { get; } = [];

    // Layout properties
    public Position Position { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }

    public bool IsComposite => NestedStates.Count > 0;
}