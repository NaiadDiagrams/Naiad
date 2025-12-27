using MermaidSharp.Core.Models;

namespace MermaidSharp.Diagrams.State;

public class StateModel : DiagramBase
{
    public List<State> States { get; } = [];
    public List<StateTransition> Transitions { get; } = [];
    public List<StateNote> Notes { get; } = [];
}

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

public enum StateType
{
    Normal,
    Start,      // [*] as source
    End,        // [*] as target
    Fork,       // <<fork>>
    Join,       // <<join>>
    Choice      // <<choice>>
}

public class StateTransition
{
    public required string FromId { get; init; }
    public required string ToId { get; init; }
    public string? Label { get; set; }

    // Layout properties
    public List<Position> Points { get; } = [];
}

public class StateNote
{
    public required string Text { get; init; }
    public required string StateId { get; init; }
    public NotePosition Position { get; set; } = NotePosition.RightOf;
}

public enum NotePosition
{
    LeftOf,
    RightOf
}
