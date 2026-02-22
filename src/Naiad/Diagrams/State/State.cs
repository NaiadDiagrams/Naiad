namespace MermaidSharp.Diagrams.State;

public class State
{
    public required string Id { get; init; }
    public string? Description { get; set; }
    public StateType Type { get; set; } = StateType.Normal;
    public List<State> NestedStates { get; } = [];
    public List<StateTransition> NestedTransitions { get; } = [];
    public string? CssClass { get; set; }

    // Layout properties
    public Position Position { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }

    public bool IsSpecial => Type is StateType.Start or StateType.End or StateType.Fork or StateType.Join or StateType.Choice;
    public bool IsComposite => NestedStates.Count > 0;
}